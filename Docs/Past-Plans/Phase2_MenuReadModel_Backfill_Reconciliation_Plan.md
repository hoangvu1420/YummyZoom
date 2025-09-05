# Phase 2: Menu Read Model Backfill & Reconciliation Plan

This document details the implementation plan for the one-shot backfill and periodic reconciliation hosted service for `FullMenuView`, leveraging the existing `IMenuReadModelRebuilder` and infrastructure patterns.

## Goals

- Populate `FullMenuViews` for existing restaurants (seeded or imported) where no projections were emitted.
- Periodically reconcile to repair missed/failed projections and refresh stale rows.
- Keep public read endpoints fast and reliable by ensuring pre-baked JSON exists and is current.

## Why This Is Needed

- Seed data clears domain events; no projector runs → empty `FullMenuViews` until mutations occur.
- Operational resilience: catch-up after outages, handler failures, or schema/logic drift in the rebuilder.
- Public SLA: `GET /api/public/restaurants/{restaurantId}/menu` depends on `FullMenuView` freshness.

References:
- Rebuilder API: `src/Application/Restaurants/Queries/Common/IMenuReadModelRebuilder.cs`
- Implementation: `src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs`
- Storage model/index: `src/Infrastructure/Data/Models/FullMenuView.cs`, `src/Infrastructure/Data/Configurations/FullMenuViewConfiguration.cs`
- Hosted service pattern: `src/Infrastructure/Outbox/OutboxPublisherHostedService.cs`

## Scope

- Implement a background service that:
  - Performs a one-time backfill on startup (optional initial delay).
  - Periodically reconciles by sweeping restaurants ordered by `LastRebuiltAt` (oldest first).
  - Cleans up orphaned views when no enabled menu exists.
- Make behavior configurable and safe to run continuously in production.

## Non‑Goals

- No partial JSON patching (future optimization).
- No read model schema migration logic beyond current `MenuJson` + `LastRebuiltAt`.
- No public/admin API surface for triggering the job (optional later).

## Configuration (Options)

Create `MenuReadModelMaintenanceOptions`:
- `Enabled` (bool, default true in Production)
- `InitialDelay` (TimeSpan; start-up backoff)
- `BackfillBatchSize` (int; e.g., 100)
- `ReconBatchSize` (int; e.g., 100)
- `ReconInterval` (TimeSpan; e.g., 00:05:00)
- `MaxParallelism` (int; e.g., 4–8)
- `DeleteOrphans` (bool; default true)
- `LogEveryN` (int; progress interval)

Example `appsettings.json` fragment:
```json
{
  "ReadModelMaintenance": {
    "Enabled": true,
    "InitialDelay": "00:00:15",
    "BackfillBatchSize": 200,
    "ReconBatchSize": 200,
    "ReconInterval": "00:05:00",
    "MaxParallelism": 6,
    "DeleteOrphans": true,
    "LogEveryN": 50
  }
}
```

## Service Design

Add `MenuReadModelMaintenanceHostedService : BackgroundService` in `Infrastructure`.

Dependencies:
- `IOptions<MenuReadModelMaintenanceOptions>`
- `IDbConnectionFactory` (Dapper)
- `IMenuReadModelRebuilder`
- `ILogger<MenuReadModelMaintenanceHostedService>`
- `TimeProvider` (optional; consistency with other services)

Lifecycle:
1) Optional initial delay if configured.
2) One-shot backfill pass:
   - Build missing rows for restaurants with enabled, non-deleted menus where no `FullMenuView` exists.
   - Delete orphaned views (no enabled menu) if `DeleteOrphans`.
3) Periodic loop until cancellation:
   - Sweep oldest `FullMenuViews` by `LastRebuiltAt` ascending (limit `ReconBatchSize`).
   - Rebuild+upsert for chosen restaurant IDs, limited by `MaxParallelism`.
   - Optionally re-check missing/orphaned small batches each cycle.

Idempotency & Safety:
- `UpsertAsync` and `DeleteAsync` are idempotent; service can retry work safely.
- Per-restaurant try/catch; continue on errors; log failure counts.
- Concurrency controlled via `Parallel.ForEachAsync` or `SemaphoreSlim`.

## Selection Queries (Dapper)

Use `IDbConnectionFactory.CreateConnection()`; keep SQL simple and indexed.

- Missing rows (enabled menu exists, but no view):
```sql
SELECT r."Id" AS "RestaurantId"
FROM "Restaurants" r
JOIN "Menus" m ON m."RestaurantId" = r."Id"
WHERE m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
  AND r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "FullMenuViews" v WHERE v."RestaurantId" = r."Id"
  )
LIMIT @BatchSize;
```

- Orphaned views (view exists, but no enabled menu):
```sql
SELECT v."RestaurantId"
FROM "FullMenuViews" v
LEFT JOIN "Menus" m
  ON m."RestaurantId" = v."RestaurantId"
  AND m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
WHERE m."Id" IS NULL
LIMIT @BatchSize;
```

- Stale rows (oldest first):
```sql
SELECT v."RestaurantId"
FROM "FullMenuViews" v
ORDER BY v."LastRebuiltAt" ASC
LIMIT @BatchSize;
```

Edge Cases:
- `RebuildAsync` throws when no enabled menu exists; treat as a signal to delete the view if present and/or skip.

## Implementation Steps

1. Add options class
```csharp
// src/Infrastructure/Outbox/MenuReadModelMaintenanceOptions.cs (or a new folder e.g., Infrastructure/ReadModels/Common)
public sealed class MenuReadModelMaintenanceOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;
    public int BackfillBatchSize { get; set; } = 100;
    public int ReconBatchSize { get; set; } = 100;
    public TimeSpan ReconInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxParallelism { get; set; } = 4;
    public bool DeleteOrphans { get; set; } = true;
    public int LogEveryN { get; set; } = 50;
}
```

2. Add hosted service skeleton
```csharp
// src/Infrastructure/ReadModels/FullMenu/MenuReadModelMaintenanceHostedService.cs
using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Infrastructure.Data;

public sealed class MenuReadModelMaintenanceHostedService : BackgroundService
{
    private readonly MenuReadModelMaintenanceOptions _opt;
    private readonly IDbConnectionFactory _db;
    private readonly IMenuReadModelRebuilder _rebuilder;
    private readonly ILogger<MenuReadModelMaintenanceHostedService> _logger;

    public MenuReadModelMaintenanceHostedService(
        IOptions<MenuReadModelMaintenanceOptions> opt,
        IDbConnectionFactory db,
        IMenuReadModelRebuilder rebuilder,
        ILogger<MenuReadModelMaintenanceHostedService> logger)
    {
        _opt = opt.Value;
        _db = db;
        _rebuilder = rebuilder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opt.Enabled)
        {
            _logger.LogInformation("MenuReadModelMaintenance: disabled via config");
            return;
        }

        if (_opt.InitialDelay > TimeSpan.Zero)
            await Task.Delay(_opt.InitialDelay, stoppingToken);

        // One-shot backfill
        await BackfillOnceAsync(stoppingToken);

        // Periodic reconciliation loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await ReconcileOnceAsync(stoppingToken);
            await Task.Delay(_opt.ReconInterval, stoppingToken);
        }
    }

    private async Task BackfillOnceAsync(CancellationToken ct)
    {
        try
        {
            using var conn = _db.CreateConnection();

            // Missing rows
            var missing = await conn.QueryAsync<Guid>("""
                SELECT r."Id"
                FROM "Restaurants" r
                JOIN "Menus" m ON m."RestaurantId" = r."Id"
                WHERE m."IsEnabled" = TRUE AND m."IsDeleted" = FALSE AND r."IsDeleted" = FALSE
                  AND NOT EXISTS (SELECT 1 FROM "FullMenuViews" v WHERE v."RestaurantId" = r."Id")
                LIMIT @Batch;
            """, new { Batch = _opt.BackfillBatchSize });

            await ProcessIdsAsync(missing, ct);

            if (_opt.DeleteOrphans)
            {
                var orphans = await conn.QueryAsync<Guid>("""
                    SELECT v."RestaurantId"
                    FROM "FullMenuViews" v
                    LEFT JOIN "Menus" m ON m."RestaurantId" = v."RestaurantId" AND m."IsEnabled" = TRUE AND m."IsDeleted" = FALSE
                    WHERE m."Id" IS NULL
                    LIMIT @Batch;
                """, new { Batch = _opt.BackfillBatchSize });

                await DeleteIdsAsync(orphans, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MenuReadModelMaintenance: backfill failed");
        }
    }

    private async Task ReconcileOnceAsync(CancellationToken ct)
    {
        try
        {
            using var conn = _db.CreateConnection();

            var stale = await conn.QueryAsync<Guid>("""
                SELECT v."RestaurantId"
                FROM "FullMenuViews" v
                ORDER BY v."LastRebuiltAt" ASC
                LIMIT @Batch;
            """, new { Batch = _opt.ReconBatchSize });

            await ProcessIdsAsync(stale, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MenuReadModelMaintenance: reconciliation pass failed");
        }
    }

    private async Task ProcessIdsAsync(IEnumerable<Guid> restaurantIds, CancellationToken ct)
    {
        var counter = 0;
        using var throttler = new SemaphoreSlim(_opt.MaxParallelism);
        var tasks = new List<Task>();

        foreach (var id in restaurantIds)
        {
            await throttler.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var (json, rebuiltAt) = await _rebuilder.RebuildAsync(id, ct);
                    await _rebuilder.UpsertAsync(id, json, rebuiltAt, ct);

                    var n = Interlocked.Increment(ref counter);
                    if (_opt.LogEveryN > 0 && n % _opt.LogEveryN == 0)
                        _logger.LogInformation("MenuReadModelMaintenance: processed {Count} restaurants", n);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Enabled menu not found"))
                {
                    // No enabled menu → ensure view is removed
                    await _rebuilder.DeleteAsync(id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MenuReadModelMaintenance: failed processing RestaurantId={RestaurantId}", id);
                }
                finally
                {
                    throttler.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
    }

    private async Task DeleteIdsAsync(IEnumerable<Guid> restaurantIds, CancellationToken ct)
    {
        foreach (var id in restaurantIds)
        {
            try { await _rebuilder.DeleteAsync(id, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "MenuReadModelMaintenance: failed deleting RestaurantId={RestaurantId}", id); }
        }
    }
}
```

3. Wire up DI
```csharp
// src/Infrastructure/DependencyInjection.cs
// In AddInfrastructureServices:
builder.Services.Configure<MenuReadModelMaintenanceOptions>(builder.Configuration.GetSection("ReadModelMaintenance"));
// Toggle per environment if desired (e.g., disable in Development/Test by default):
// var env = builder.Environment; // if available in your builder wrapper
// builder.Services.PostConfigure<MenuReadModelMaintenanceOptions>(o => { if (env.IsEnvironment("Test")) o.Enabled = false; });

builder.Services.AddHostedService<MenuReadModelMaintenanceHostedService>();
```

4. Tests and local runs
- Disable the service in tests by removing it in test host configuration (similar to OutboxPublisherHostedService) or via config (`Enabled = false`).
  - `tests/Web.ApiContractTests/Infrastructure/ApiContractWebAppFactory.cs`: remove `IHostedService` with `ImplementationType.Name == "MenuReadModelMaintenanceHostedService"`.
- For local development, set `ReadModelMaintenance.Enabled = false` unless you want periodic sweeps during manual testing.

5. Observability
- Log counts and per-pass duration at `Information` level; errors at `Warning` (per ID) and `Error` (pass-level failures).
- Optional: add basic event counters or metrics hooks later if needed.

6. Rollout Plan
- Ship with `Enabled = false` by default in non-production; true in Production (config-driven).
- On first production deployment, monitor DB load; tune `BatchSize`/`MaxParallelism`/`ReconInterval` accordingly.
- Validate that backfill completes for existing restaurants; verify public endpoint latency and hit ratio.

## Notable Points & Edge Cases

- Restaurants without enabled menus: `RebuildAsync` throws; delete any lingering view and skip.
- Orphan cleanup: safe and idempotent; can be run every cycle with small batches.
- Concurrency with event handlers: benign duplicates; `UPSERT` ensures last write wins.
- Staleness heuristic: `LastRebuiltAt` index supports efficient oldest-first sweeps; future: compute source max `LastModified` to skip no-op rebuilds.
- Time source: use system time (rebuilder already sets `LastRebuiltAt`); switchable to `TimeProvider` later if needed.
- Configuration-first: All intensity controls are tunable without code changes; default values conservative.

## Future Enhancements

- Compare a hash/version of the composed JSON to skip no-op upserts.
- Partition sweeps by tenant scale (shard by restaurant ID modulo N) if needed.
- Add an admin-only API endpoint to trigger rebuild for a single restaurant.
- Move selection logic into a dedicated maintainer service for testability and reuse.

---

This plan follows existing patterns in the repository (BackgroundService, Dapper selection, DI options, idempotent upsert/delete) and keeps complexity low while covering operational needs.

---

## Verification & Observability

Automate verification as much as possible to avoid manual-only checks.

- Structured logs: log per-pass start/end, duration, counts for backfilled/reconciled/deleted, and per-restaurant warnings on failures; errors for pass-level failures. Respect `LogEveryN` for progress.
- Optional metrics: add basic counters/gauges if you have a metrics sink (for example: `menu_readmodel_backfilled_total`, `menu_readmodel_reconciled_total`, `menu_readmodel_orphans_deleted_total`, `menu_readmodel_pass_duration_ms`).

Health SQL (for dashboards or ad-hoc checks):

1) Missing views (enabled menu exists, no FullMenuView):
```sql
SELECT r."Id" AS "RestaurantId"
FROM "Restaurants" r
JOIN "Menus" m ON m."RestaurantId" = r."Id"
WHERE m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
  AND r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "FullMenuViews" v WHERE v."RestaurantId" = r."Id"
  );
```

2) Orphaned views (view exists, but no enabled menu):
```sql
SELECT v."RestaurantId"
FROM "FullMenuViews" v
LEFT JOIN "Menus" m
  ON m."RestaurantId" = v."RestaurantId"
  AND m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
WHERE m."Id" IS NULL;
```

3) Stale views older than a threshold (example: 1 day):
```sql
SELECT COUNT(*)
FROM "FullMenuViews"
WHERE "LastRebuiltAt" < now() - interval '1 day';
```

Suggested smoke procedure per environment:
- Capture baseline counts for “missing” and “orphans”.
- Enable the service; wait at least one `ReconInterval` and a bit of slack.
- Re-run counts; expect missing to drop toward 0 (or steadily decrease), orphans 0.
- Spot-check a few restaurants via the public GET menu; confirm HTTP 200 with ETag/Last-Modified and plausible JSON.

Improving testability without end-to-end tests:
- Keep the hosted loop disabled in tests (`Enabled=false`) or remove service in test host setup.
- Factor selection/processing into a callable class or keep `BackfillOnceAsync`/`ReconcileOnceAsync` internal and invoke from an integration test with a real DB and a fake `IMenuReadModelRebuilder` if desired.

Safety toggles and guards:
- Default `Enabled=false` in Test/Dev; enable on-demand locally for one-shot validation.
- Use `MaxParallelism`, `BatchSize`, and `LogEveryN` to bound load and increase observability.
- Config-driven behavior; no code changes needed to tune intensity.

---

## Proposal: Backfill & Reconciliation for Other Read Models

Now that `FullMenuView` maintenance is implemented and verified, we should extend the same operational guarantees to two additional read models that power critical user experiences: `SearchIndexItems` (universal search + autocomplete) and `RestaurantReviewSummaries` (ratings and counts).

### SearchIndexItems

Current state:
- Maintained incrementally via event handlers (restaurant created/verified/geo/business-hours/accepting-orders; menu item created/updated/deleted; review events cascade rating counts). See `src/Application/Search/EventHandlers/*` and `src/Application/Reviews/EventHandlers/*`.
- Maintainer: `ISearchReadModelMaintainer` implemented by `SearchIndexMaintainer` with `UpsertRestaurantByIdAsync`, `UpsertMenuItemByIdAsync`, soft-delete, and a restaurant-scoped `RebuildRestaurantAsync`. Rows carry `SourceVersion` and `UpdatedAt` to enforce last-write-wins.

Why a maintenance job helps:
- Seeded/imported data predates projectors → no index rows yet.
- Missed outbox deliveries or transient failures can leave gaps or stale rows.
- Over time, we want to opportunistically sweep “oldest” rows to heal any drift (e.g., parsing/business-hours logic updates).

Design: `SearchIndexMaintenanceHostedService`
- Options (mirrors menu options; separate section `SearchIndexMaintenance`):
  - `Enabled`, `InitialDelay`, `BackfillBatchSize`, `ReconBatchSize`, `ReconInterval`, `MaxParallelism`, `DeleteOrphans`, `LogEveryN`.
- Backfill pass (Dapper selection → maintainer upserts):
  - Missing restaurants:
    - SELECT r.Id FROM "Restaurants" r WHERE r."IsDeleted" = FALSE AND NOT EXISTS (SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = r."Id") LIMIT @Batch;
    - For each → `_search.UpsertRestaurantByIdAsync(r.Id, now.Ticks)`.
  - Missing menu items:
    - SELECT i.Id FROM "MenuItems" i JOIN "Restaurants" r ON r."Id" = i."RestaurantId" WHERE i."IsDeleted" = FALSE AND r."IsDeleted" = FALSE AND NOT EXISTS (SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = i."Id") LIMIT @Batch;
    - For each → `_search.UpsertMenuItemByIdAsync(i.Id, now.Ticks)`.
  - Orphans (soft-delete):
    - Restaurants: SELECT s."Id" FROM "SearchIndexItems" s LEFT JOIN "Restaurants" r ON r."Id" = s."Id" WHERE s."Type" = 'restaurant' AND (r."Id" IS NULL OR r."IsDeleted") LIMIT @Batch;
    - Menu items: SELECT s."Id" FROM "SearchIndexItems" s LEFT JOIN "MenuItems" i ON i."Id" = s."Id" WHERE s."Type" = 'menu_item' AND (i."Id" IS NULL OR i."IsDeleted") LIMIT @Batch;
    - For each → `_search.SoftDeleteByIdAsync(id, now.Ticks)`.
- Reconciliation loop (periodic):
  - Sweep “oldest updated” restaurants to refresh derived flags and cascade to items:
    - SELECT r."Id" FROM "SearchIndexItems" s JOIN "Restaurants" r ON r."Id" = s."Id" WHERE s."Type"='restaurant' AND s."SoftDeleted"=FALSE ORDER BY s."UpdatedAt" ASC LIMIT @Batch;
    - For each → `_search.UpsertRestaurantByIdAsync(r.Id, now.Ticks)`.
  - Optionally sweep “oldest updated” menu items to refresh names/descriptions that wouldn’t be refreshed by the restaurant cascade:
    - SELECT i."Id" FROM "SearchIndexItems" s JOIN "MenuItems" i ON i."Id" = s."Id" WHERE s."Type"='menu_item' AND s."SoftDeleted"=FALSE ORDER BY s."UpdatedAt" ASC LIMIT @Batch;
    - For each → `_search.UpsertMenuItemByIdAsync(i.Id, now.Ticks)`.
- Concurrency/observability: same pattern as the menu job (`SemaphoreSlim`, per-pass counts and timings, `LogEveryN`).
- Idempotency: enforced by `SourceVersion` check in `SearchIndexMaintainer` (`WHERE existing.SourceVersion <= EXCLUDED.SourceVersion`). Using `TimeProvider.GetUtcNow().Ticks` for rebuild passes is sufficient.

DI/config wiring:
- Add `SearchIndexMaintenanceOptions` (same shape as menu options) under `"SearchIndexMaintenance"` in appsettings.
- Register hosted service and options alongside `ISearchReadModelMaintainer` in `src/Infrastructure/DependencyInjection.cs`.

Notes:
- The existing `UpsertRestaurantByIdAsync` already cascades key flags/geo/cuisine to the restaurant’s menu items, reducing the need to sweep all items every pass; still, include a slow, small-batch item sweep to catch content changes (name/description) that only item events would set.
- If needed later, add a bulk `RebuildAsync()` to `SearchIndexMaintainer`; not required for MVP of the maintenance job.

### RestaurantReviewSummaries

Current state:
- Maintained on review lifecycle events via `IReviewSummaryMaintainer` (`RecomputeForRestaurantAsync`, `RecomputeForReviewAsync`). Implementation uses a simple CTE to upsert average and count per restaurant. Consumers (e.g., search index) already left-join this table.

Why a maintenance job helps:
- Seed/import scenarios or missed events leave the table empty or stale, affecting ratings in search and any public summaries.
- This is a small, cheap aggregate that can be recomputed in bulk quickly.

Design: `ReviewSummaryMaintenanceHostedService`
- Options: `Enabled`, `InitialDelay`, `ReconInterval` (batch sizing is less critical if using a single set-based statement), `LogEveryN` optional.
- Backfill + reconciliation (same statement does both; runs on a schedule):
  - One-shot, set-based upsert (fast and idempotent):
    - INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
      SELECT r."Id",
             COALESCE(AVG(rv."Rating")::double precision, 0.0) AS avg_rating,
             COALESCE(COUNT(rv.*)::int, 0) AS total_reviews
      FROM "Restaurants" r
      LEFT JOIN "Reviews" rv ON rv."RestaurantId" = r."Id" AND rv."IsDeleted" = FALSE AND rv."IsHidden" = FALSE
      WHERE r."IsDeleted" = FALSE
      GROUP BY r."Id"
      ON CONFLICT ("RestaurantId") DO UPDATE
        SET "AverageRating" = EXCLUDED."AverageRating",
            "TotalReviews"  = EXCLUDED."TotalReviews";
  - This covers: create missing, update stale, and ensures every non-deleted restaurant has a row (0/0 when no visible reviews).
- Alternative (if we prefer the per-restaurant API):
  - Select distinct restaurant IDs from `Reviews` (optionally restricted to recent `LastModified`) and call `_summary.RecomputeForRestaurantAsync(id, now.Ticks)` in small batches. The set-based SQL above is simpler and faster.

DI/config wiring:
- Add `ReviewSummaryMaintenanceOptions` with `Enabled`, `InitialDelay`, `ReconInterval` under `"ReviewSummaryMaintenance"`.
- Register hosted service after `IReviewSummaryMaintainer` in `src/Infrastructure/DependencyInjection.cs`.

Notes:
- No `SourceVersion` column here; recomputation is deterministic and idempotent, so last-write-wins semantics are unnecessary.
- If we later add a `LastComputedAt` column for observability, we can sweep “oldest computed” first; not required initially.

### Rollout Recommendation
- Implement both maintenance services behind config flags. Default to `Enabled = false` for local/test, `true` in production once vetted.
- Start conservatively: `BackfillBatchSize = 100`, `ReconBatchSize = 100`, `MaxParallelism = 4`, `ReconInterval = 5–10 minutes` for SearchIndex; `ReconInterval = 10–15 minutes` (one statement) for ReviewSummary.
- Monitor DB load and job timings; tune upward as comfortable.

### Open Questions (to confirm before coding)
- Should the search index include menu items from restaurants with disabled menus? Current maintainer does not check menu enabled; if we want to hide those, we can add a join to `Menus(IsEnabled)` in the backfill queries and in `UpsertMenuItemByIdAsync`.
- Any additional filters for restaurants (e.g., only verified) in public search? If yes, decide whether to filter at query time or exclude at indexing time.
- For ReviewSummary, do we want to exclude very new or unverified restaurants? The SQL above includes all non-deleted restaurants and is simplest.

With these two services, all three critical read models (`FullMenuView`, `SearchIndexItems`, `RestaurantReviewSummaries`) gain the same resilience guarantees: backfilled after seed/import, self-healing from missed events, and periodically refreshed to match evolving business logic.
