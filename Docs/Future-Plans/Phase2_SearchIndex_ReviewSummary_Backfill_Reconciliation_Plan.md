# Phase 2: SearchIndex & ReviewSummary Backfill/Reconciliation Plan

This document outlines a concrete, implementation-ready plan to add maintenance jobs for two important read models: `SearchIndexItems` and `RestaurantReviewSummaries`. It follows the proven pattern used by the `FullMenuView` maintenance job, adapted to each model’s data shape, write paths, and operational requirements.

## Goals

- Seed/import compatible: populate read models for existing data where no projectors were run.
- Self-healing: reconcile gaps and stale rows caused by transient failures or logic evolution.
- Tunable operations: control intensity via options; enable/disable per-job independently.
- Operational clarity: strong observability, safe idempotent upserts, and bounded concurrency.

## Scope (What we will build)

- `SearchIndexMaintenanceHostedService`: backfill + periodic reconciliation for `SearchIndexItems` (restaurants and menu items), with orphan soft-deletes.
- `ReviewSummaryMaintenanceHostedService`: backfill + periodic recompute for `RestaurantReviewSummaries` using a single set-based UPSERT (fast/idempotent).
- Two dedicated Options classes and appsettings sections with sensible defaults.
- DI wiring and production-ready logging.

Non-goals:
- No migration of read model schemas (we use existing tables/columns).
- No public/admin endpoints (can be added later if needed).

---

## Current State Inventory

- Search Index
  - Read model: `src/Infrastructure/Data/Models/SearchIndexItem.cs`
  - Mapping/Indexes: `src/Infrastructure/Data/Configurations/SearchIndexItemConfiguration.cs`
  - Maintainer: `src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs` with APIs:
    - `UpsertRestaurantByIdAsync`, `UpsertMenuItemByIdAsync`, `SoftDeleteByIdAsync`, and a `RebuildRestaurantAsync` helper.
    - Idempotency via `SourceVersion` (last-write-wins with `WHERE existing.SourceVersion <= EXCLUDED.SourceVersion`).
  - Writers: event handlers in `src/Application/Search/EventHandlers/*` and review-driven updates in `src/Application/Reviews/EventHandlers/*`.
  - Query consumers: Universal search and Autocomplete queries (Dapper) rely on this table.

- Review Summaries
  - Read model: `src/Infrastructure/Data/Models/RestaurantReviewSummary.cs`
  - Maintainer: `src/Infrastructure/ReadModels/Reviews/ReviewSummaryMaintainer.cs` with APIs:
    - `RecomputeForRestaurantAsync` (CTE + UPSERT) and `RecomputeForReviewAsync`.
  - Writers: review lifecycle handlers; readers: search index join and any public ratings displays.

---

## Options (Configuration)

Define separate options for independent control/tuning.

1) SearchIndexMaintenanceOptions (new)
- `Enabled` (bool) – default false in Dev/Test, true in Prod once vetted.
- `InitialDelay` (TimeSpan) – start-up backoff.
- `BackfillBatchSize` (int) – missing/orphan batches.
- `ReconBatchSize` (int) – reconciliation batches.
- `ReconInterval` (TimeSpan) – periodic sweep cadence.
- `MaxParallelism` (int) – concurrent upsert throttle.
- `DeleteOrphans` (bool) – whether to soft-delete orphaned entries.
- `LogEveryN` (int) – progress logging cadence.

2) ReviewSummaryMaintenanceOptions (new)
- `Enabled` (bool)
- `InitialDelay` (TimeSpan)
- `ReconInterval` (TimeSpan)
- (Optional) `BatchSize`/`MaxParallelism` only if doing per-restaurant API; not needed for set-based UPSERT.

Example appsettings (Development):
```
"SearchIndexMaintenance": {
  "Enabled": false,
  "InitialDelay": "00:00:10",
  "BackfillBatchSize": 100,
  "ReconBatchSize": 100,
  "ReconInterval": "00:05:00",
  "MaxParallelism": 4,
  "DeleteOrphans": true,
  "LogEveryN": 50
},
"ReviewSummaryMaintenance": {
  "Enabled": false,
  "InitialDelay": "00:00:10",
  "ReconInterval": "00:15:00"
}
```

---

## Hosted Service Design – SearchIndexMaintenanceHostedService

Dependencies:
- `IOptions<SearchIndexMaintenanceOptions>`
- `IDbConnectionFactory` (Dapper)
- `ISearchReadModelMaintainer` (existing)
- `TimeProvider` (consistent source for source version ticks)
- `ILogger<SearchIndexMaintenanceHostedService>`

Lifecycle (ExecuteAsync):
1) Honor `Enabled` and optional `InitialDelay`.
2) One-shot Backfill pass:
   - Build missing restaurant rows.
   - Build missing menu item rows.
   - Soft-delete orphans (when enabled).
3) Periodic loop until cancelled (interval = `ReconInterval`):
   - Reconcile restaurants by sweeping “oldest updated” first.
   - Optionally sweep some menu items (small batches) to refresh item-specific fields.

Idempotency & Concurrency:
- Use `_timeProvider.GetUtcNow().Ticks` as `sourceVersion` for maintenance upserts.
- Throttle work with `SemaphoreSlim(maxParallelism)`; per-ID try/catch; continue on error.
- Leverage maintainer’s last-write-wins logic.

Selection SQL (Dapper):
- Missing restaurants:
```sql
SELECT r."Id"
FROM "Restaurants" r
WHERE r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = r."Id" AND s."Type" = 'restaurant'
  )
LIMIT @Batch;
```

- Missing menu items:
```sql
SELECT i."Id"
FROM "MenuItems" i
JOIN "Restaurants" r ON r."Id" = i."RestaurantId"
WHERE i."IsDeleted" = FALSE AND r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = i."Id" AND s."Type" = 'menu_item'
  )
LIMIT @Batch;
```

- Orphan restaurants (soft-delete):
```sql
SELECT s."Id"
FROM "SearchIndexItems" s
LEFT JOIN "Restaurants" r ON r."Id" = s."Id"
WHERE s."Type" = 'restaurant'
  AND (r."Id" IS NULL OR r."IsDeleted" = TRUE)
LIMIT @Batch;
```

- Orphan menu items (soft-delete):
```sql
SELECT s."Id"
FROM "SearchIndexItems" s
LEFT JOIN "MenuItems" i ON i."Id" = s."Id"
WHERE s."Type" = 'menu_item'
  AND (i."Id" IS NULL OR i."IsDeleted" = TRUE)
LIMIT @Batch;
```

- Reconcile restaurants (oldest updated first):
```sql
SELECT r."Id"
FROM "SearchIndexItems" s
JOIN "Restaurants" r ON r."Id" = s."Id"
WHERE s."Type" = 'restaurant' AND s."SoftDeleted" = FALSE
ORDER BY s."UpdatedAt" ASC
LIMIT @Batch;
```

- Reconcile menu items (optional, oldest updated first):
```sql
SELECT i."Id"
FROM "SearchIndexItems" s
JOIN "MenuItems" i ON i."Id" = s."Id"
WHERE s."Type" = 'menu_item' AND s."SoftDeleted" = FALSE
ORDER BY s."UpdatedAt" ASC
LIMIT @Batch;
```

Processing loop per ID:
- Restaurants → `_search.UpsertRestaurantByIdAsync(id, sv, ct)`; this also cascades flags/geo/cuisine to items.
- Menu items → `_search.UpsertMenuItemByIdAsync(id, sv, ct)`.
- Orphans → `_search.SoftDeleteByIdAsync(id, sv, ct)`.

Edge decisions:
- If we want to exclude items for disabled menus, add a `Menus(IsEnabled=TRUE)` join in the missing and reconcile queries, and optionally in maintainer logic.
- If we want to restrict to verified restaurants in public search, keep index inclusive but filter at query-time (current approach), or enforce at indexing time—decide explicitly.

---

## Hosted Service Design – ReviewSummaryMaintenanceHostedService

Dependencies:
- `IOptions<ReviewSummaryMaintenanceOptions>`
- `IDbConnectionFactory` (Dapper)
- `ILogger<ReviewSummaryMaintenanceHostedService>`

Approach: set-based UPSERT across all restaurants (fast and idempotent). Alternative per-restaurant API is supported but not recommended initially.

Lifecycle (ExecuteAsync):
1) Honor `Enabled` and optional `InitialDelay`.
2) Periodic loop until cancelled (interval = `ReconInterval`):
   - Execute a single UPSERT statement that recomputes all restaurants in one pass.

Core SQL:
```sql
INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
SELECT r."Id",
       COALESCE(AVG(rv."Rating")::double precision, 0.0) AS avg_rating,
       COALESCE(COUNT(rv.*)::int, 0) AS total_reviews
FROM "Restaurants" r
LEFT JOIN "Reviews" rv
  ON rv."RestaurantId" = r."Id"
 AND rv."IsDeleted" = FALSE
 AND rv."IsHidden" = FALSE
WHERE r."IsDeleted" = FALSE
GROUP BY r."Id"
ON CONFLICT ("RestaurantId") DO UPDATE
  SET "AverageRating" = EXCLUDED."AverageRating",
      "TotalReviews"  = EXCLUDED."TotalReviews";
```

Notes:
- This covers missing and stale rows in one pass, including 0/0 rows for restaurants with no visible reviews.
- No `SourceVersion` needed; recomputation is deterministic.
- Optional enhancement: add a `LastComputedAt` column for observability and order-by oldest-first sweeps later.

---

## DI Wiring (Infrastructure)

In `src/Infrastructure/DependencyInjection.cs`:
- Register options:
  - `builder.Services.Configure<SearchIndexMaintenanceOptions>(builder.Configuration.GetSection("SearchIndexMaintenance"));`
  - `builder.Services.Configure<ReviewSummaryMaintenanceOptions>(builder.Configuration.GetSection("ReviewSummaryMaintenance"));`
- Register hosted services:
  - `builder.Services.AddHostedService<SearchIndexMaintenanceHostedService>();`
  - `builder.Services.AddHostedService<ReviewSummaryMaintenanceHostedService>();`

Ensure `ISearchReadModelMaintainer` and `IReviewSummaryMaintainer` are registered (already present).

---

## Observability

- Log per-pass start/end, duration, counts (built/soft-deleted/reconciled) at Information.
- Log per-ID failures at Warning; pass-level failures at Error.
- `LogEveryN` progress for SearchIndex maintenance.
- Optional metrics counters and durations if a metrics sink exists.

Health SQL (ad-hoc checks):
- Search index missing restaurants:
```sql
SELECT COUNT(*)
FROM "Restaurants" r
WHERE r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = r."Id" AND s."Type"='restaurant'
  );
```

- Search index missing menu items:
```sql
SELECT COUNT(*)
FROM "MenuItems" i
JOIN "Restaurants" r ON r."Id" = i."RestaurantId"
WHERE i."IsDeleted" = FALSE AND r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = i."Id" AND s."Type"='menu_item'
  );
```

- Search index orphans (restaurants and menu items):
```sql
SELECT COUNT(*) FROM (
  SELECT s."Id"
  FROM "SearchIndexItems" s LEFT JOIN "Restaurants" r ON r."Id" = s."Id"
  WHERE s."Type"='restaurant' AND (r."Id" IS NULL OR r."IsDeleted")
  UNION ALL
  SELECT s."Id"
  FROM "SearchIndexItems" s LEFT JOIN "MenuItems" i ON i."Id" = s."Id"
  WHERE s."Type"='menu_item' AND (i."Id" IS NULL OR i."IsDeleted")
) x;
```

- Review summaries missing for existing restaurants:
```sql
SELECT COUNT(*)
FROM "Restaurants" r
WHERE r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "RestaurantReviewSummaries" s WHERE s."RestaurantId" = r."Id"
  );
```

---

## Testing Strategy

- Unit-ish: verify selection SQL fragments and branching logic (small in-memory tests if practical).
- Integration (preferred): with a Postgres test DB
  - Seed Restaurants/MenuItems/Reviews in various states (deleted/hidden/disabled parents).
  - Run backfill once; assert missing counts drop; rows created.
  - Toggle DeleteOrphans; create orphans; assert SoftDeleted or removed as intended.
  - Modify source data; run reconcile; assert updated fields and timestamps advance.
  - Validate idempotency by re-running passes; no duplicate effects.
- Web/API unaffected functionally; search/ratings queries should reflect updated read models.

---

## Rollout & Tuning

- Default `Enabled=false` in Dev/Test; enable manually for validation.
- In Production, enable with conservative defaults:
  - SearchIndex: Backfill/Recon batch = 100, MaxParallelism = 4, ReconInterval = 5–10 minutes.
  - ReviewSummary: ReconInterval = 10–15 minutes (single statement); no parallelism needed.
- Monitor DB load, pass durations, and error logs; tune upward as comfortable.

---

## Edge Cases & Safety

- Concurrency with event handlers is benign; UPSERT with SourceVersion (search) or deterministic recompute (reviews) ensures last-write-wins or equivalent.
- Large datasets: keep batches small and intervals reasonable; increase gradually.
- Schema changes: if SearchIndex shape changes, extend maintainer first; jobs call maintainers, so no duplication.
- Disabled menus policy: decide whether items for disabled menus should be indexed; adjust queries/maintainer accordingly.

---

## Step-by-Step Implementation Checklist

1) Create options classes:
   - `SearchIndexMaintenanceOptions` in `src/Infrastructure/ReadModels/Search/` (or `…/Common` if shared).
   - `ReviewSummaryMaintenanceOptions` in `src/Infrastructure/ReadModels/Reviews/`.
2) Implement `SearchIndexMaintenanceHostedService`:
   - ExecuteAsync lifecycle, backfill + reconcile passes, throttled processing, structured logs.
   - Selection queries per above; drive maintainer methods with `TimeProvider.UtcNow.Ticks`.
3) Implement `ReviewSummaryMaintenanceHostedService`:
   - ExecuteAsync lifecycle; periodic single UPSERT.
4) Wire DI:
   - Configure options sections; add both hosted services in `DependencyInjection.cs`.
5) Add appsettings sections in `src/Web/appsettings.Development.json` (and Production config template).
6) Testing:
   - Integration tests to validate counts and effects; disable services by config for unrelated tests.
7) Observability:
   - Ensure logs include counts, durations, and representative warnings.
8) Rollout:
   - Enable in staging; monitor; tune; then enable in production.

---

## Open Questions (confirm before coding)

- Indexing policy for items when parent restaurant has a disabled menu: include or exclude?
- Restrict search index to verified restaurants at indexing time or only at query time?
- For reviews, any need to exclude very new/unverified restaurants? (Default: include all non-deleted.)

If we align on these points, the implementation can proceed directly following this plan.

