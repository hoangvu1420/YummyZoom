**Purpose**
- Define how read models are built, updated, backfilled, queried, and operated in this project.
- Keep it practical: patterns, file locations, and do/don'ts.

**Design Principles**
- **CQRS split:** domain writes emit events; read models are denormalized views optimized for queries.
- **Idempotent updates:** every projector/maintainer can be retried without harm.
- **Query-first shape:** store exactly what queries need (no “clever” joins at request time).
- **Operational resilience:** background jobs backfill and reconcile; small, tunable batches.
- **Performance:** rely on DB indexes (GIN/GIST/trigram) and set-based SQL; avoid N+1.

**Catalog (Current)**
- `FullMenuView` (restaurant-scoped menu JSON snapshot)
  - Model: `src/Infrastructure/Data/Models/FullMenuView.cs`
  - Maintainer: `IMenuReadModelRebuilder` → `FullMenuViewRebuilder`
  - Job: `src/Infrastructure/ReadModels/FullMenu/MenuReadModelMaintenanceHostedService.cs`
  - Freshness: `LastRebuiltAt`; rebuilt on menu events + scheduled sweeps
  - Use: serve public menu, CDN/ETag friendly
- `SearchIndexItems` (universal search/autocomplete for restaurants and items)
  - Model: `src/Infrastructure/Data/Models/SearchIndexItem.cs`
  - Maintainer: `src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs`
  - Jobs: `src/Infrastructure/ReadModels/Search/SearchIndexMaintenanceHostedService.cs`
  - Fields: FTS (`tsvector`), trigram, geo (PostGIS), flags, rating snapshot
  - Use: `src/Application/Search/Queries/*`
- `RestaurantReviewSummaries` (per-restaurant avg/count)
  - Model: `src/Infrastructure/Data/Models/RestaurantReviewSummary.cs`
  - Maintainer: `src/Infrastructure/ReadModels/Reviews/ReviewSummaryMaintainer.cs`
  - Job: `src/Infrastructure/ReadModels/Reviews/ReviewSummaryMaintenanceHostedService.cs`
  - Use: joined by search, public summaries

**Write Paths**
- Event handlers update read models via maintainers. Examples:
  - `src/Application/Search/EventHandlers/*.cs`
  - `src/Application/Reviews/EventHandlers/*.cs`
- Maintainers encapsulate Dapper SQL and idempotency.
  - `SearchIndexMaintainer`: `UpsertRestaurantByIdAsync`, `UpsertMenuItemByIdAsync`, `SoftDeleteByIdAsync`.
  - `ReviewSummaryMaintainer`: `RecomputeForRestaurantAsync`, `RecomputeForReviewAsync`.
  - `FullMenuViewRebuilder`: `RebuildAsync` → `(json, rebuiltAt)`, then `UpsertAsync`.

**Idempotency & Ordering**
- Use a monotonic `SourceVersion` for last-write-wins when mixing sources:
  - Search index UPSERT includes `WHERE existing.SourceVersion <= EXCLUDED.SourceVersion`.
  - Recommended `SourceVersion`: event `OccurredOnUtc.Ticks`; for jobs: `TimeProvider.GetUtcNow().Ticks`.
- Pure recomputations (e.g., review summaries) don’t need `SourceVersion`.
- UPSERT always safe to retry; soft-delete uses the same last-write-wins rule.

**Backfill & Reconciliation Jobs**
- Full Menu: `MenuReadModelMaintenanceHostedService` with options `ReadModelMaintenance` in `src/Web/appsettings.Development.json`.
- Search Index: `SearchIndexMaintenanceHostedService` with `SearchIndexMaintenance` options.
- Review Summary: `ReviewSummaryMaintenanceHostedService` with `ReviewSummaryMaintenance` options.
- Options include: `Enabled`, `InitialDelay`, `BackfillBatchSize`, `ReconBatchSize`, `ReconInterval`, `MaxParallelism`, `DeleteOrphans`, `LogEveryN` (varies by job).
- DI wiring: see `src/Infrastructure/DependencyInjection.cs`.

**Query Patterns (Dapper)**
- Always select only needed columns; paginate via `DapperPagination` helpers.
- Search specifics:
  - FTS: `tsvector` + `websearch_to_tsquery('simple', unaccent(@q))`.
  - Trigram fallback and prefix handling for very short queries.
  - Geo-distance scoring with `postgis` geography.
  - See `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs` and `AutocompleteQueryHandler.cs`.
- Avoid scanning JSON; store queryable fields directly (tags, cuisine, price band, flags).

**Indexes & Storage**
- SearchIndex:
  - GIN on `TsAll`, `TsName`, `TsDescr`; trigram on `Name`, `Cuisine`; GIST on `Geo`; filters on `Type`, flags, timestamps.
  - Config: `src/Infrastructure/Data/Configurations/SearchIndexItemConfiguration.cs`.
- FullMenuView: index on `RestaurantId`, optionally on `LastRebuiltAt` for sweeps.
- ReviewSummary: PK on `RestaurantId`.

**Operational Checks (SQL snippets)**
- Search missing restaurants/items:
  - Restaurants: `NOT EXISTS` on `SearchIndexItems(Id=RestaurantId, Type='restaurant')`.
  - Items: `NOT EXISTS` on `SearchIndexItems(Id=MenuItemId, Type='menu_item')`.
- Review summaries missing: `NOT EXISTS` on `RestaurantReviewSummaries(RestaurantId)`.
- FullMenu health: see `Docs/Future-Plans/Phase2_MenuReadModel_Backfill_Reconciliation_Plan.md`.

**Configuration**
- Toggle jobs in `src/Web/appsettings.Development.json`:
  - `ReadModelMaintenance`, `SearchIndexMaintenance`, `ReviewSummaryMaintenance`.
- Production enablement via environment-specific configs; tune batch sizes and parallelism per DB capacity.

**Testing Guidance**
- Unit-level: validate selection SQL and mapping shapes.
- Integration: seed Restaurants/MenuItems/Reviews; run backfill once; assert counts drop and rows created; rerun to verify idempotency.
- Disable jobs during unrelated tests by setting `Enabled=false`.

**Troubleshooting**
- Connection lifecycle: `IDbConnectionFactory.CreateConnection()` returns a fresh pooled connection; do not cache connections.
- Event handler idempotency: handlers derive from `IdempotentNotificationHandler<T>` and rely on outbox/inbox; if you see duplicates, verify inbox logic.
- Search index drift: force a rebuild via `SearchIndexMaintainer.RebuildRestaurantAsync(restaurantId)`.
- Performance: reduce job `MaxParallelism`/batch sizes; verify indexes exist and used.

**Do / Don’t**
- Do: keep read models minimal and query-shaped; add columns when queries need them.
- Do: enforce idempotency in SQL (`UPSERT` and `SourceVersion`).
- Do: use `TimeProvider` for consistent timestamps in services.
- Don’t: parse or query inside big JSON blobs at request time.
- Don’t: share or cache `NpgsqlConnection` instances across operations.

**Key Files**
- Full Menu Job: `src/Infrastructure/ReadModels/FullMenu/MenuReadModelMaintenanceHostedService.cs`
- Search Index Maintainer + Job:
  - `src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs`
  - `src/Infrastructure/ReadModels/Search/SearchIndexMaintenanceHostedService.cs`
- Review Summary Maintainer + Job:
  - `src/Infrastructure/ReadModels/Reviews/ReviewSummaryMaintainer.cs`
  - `src/Infrastructure/ReadModels/Reviews/ReviewSummaryMaintenanceHostedService.cs`
- DI: `src/Infrastructure/DependencyInjection.cs`

