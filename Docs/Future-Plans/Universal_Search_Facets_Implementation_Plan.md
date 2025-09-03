## Universal Search — Faceted Filters Implementation Plan (Phase 1)

This plan adds faceted filtering (cuisine, tags, price bands, open‑now) to the Universal Search, with facet aggregations computed over the currently filtered result set. The approach minimizes schema changes, reuses the existing read model, and keeps latency within Phase 1 budgets.

### Objectives
- Return facet aggregations (top‑N cuisine, top‑N tags, price bands, open‑now count) alongside paged results.
- Support applying multiple facet filters and reflect them in both results and facet counts.
- Maintain p95 latency targets (< 250 ms typical dataset) using indexes and efficient SQL.

---

### Current Baseline (what exists today)
- Read model: `src/Infrastructure/Data/Models/SearchIndexItem.cs:1`
- EF config: `src/Infrastructure/Data/Configurations/SearchIndexItemConfiguration.cs:1`
- Migrations: `src/Infrastructure/Data/Migrations/20250901160952_AddSearchIndexItemsReadModel.cs:1`, `src/Infrastructure/Data/Migrations/20250902074034_AddSearchIndexItemsTsvTriggers.cs:1`
- Maintainer: `src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs:1`
- Query handler: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:1`
- Tests: `tests/Application.FunctionalTests/Features/Search/UniversalSearchTests.cs:1`, `tests/Application.FunctionalTests/Features/Search/AutocompleteTests.cs:1`

Notes:
- `Cuisine` and `Tags` columns already exist; `PriceBand` exists but is not populated yet by the maintainer (Phase 1 can ship without it populated; see Enhancements).
- Indexes exist for `Ts*` (GIN), `Name`/`Cuisine` (trigram GIN), `Tags` (GIN), and `Geo` (GiST). There is no btree index for `PriceBand`, nor an expression index for `LOWER(Cuisine)` used by equality/grouping.
- Trigger‑maintained `tsvector` columns are in place; no changes needed for facets.

---

### API Changes
- Extend request to allow additional filtering dimensions:
  - `string[]? Tags`
  - `short[]? PriceBands`
  - Keep existing `OpenNow`, `Cuisines[]`.

- Return shape: introduce a wrapper response that includes both page and facets to avoid overloading `PaginatedList<SearchResultDto>`:
  - `UniversalSearchResponseDto`
    - `PaginatedList<SearchResultDto> Page`
    - `FacetBlock Facets`
  - `FacetBlock`
    - `IReadOnlyList<FacetCount<string>> Cuisines` (top‑N)
    - `IReadOnlyList<FacetCount<string>> Tags` (top‑N)
    - `IReadOnlyList<FacetCount<short>> PriceBands`
    - `int OpenNowCount`
  - `FacetCount<T>`: `{ T Value, int Count }`

Compatibility: this is a breaking change for the search query response. Update the web endpoint and tests accordingly. Optionally add a temporary feature flag to return facets only when `includeFacets=true` to ease integration.

---

### Query Design

Core principle: compute facets over the same filtered set used for results. We will build a reusable WHERE clause and reuse it in three SQL statements executed together via Dapper `QueryMultiple`:

1) Get total count for pagination (current logic already does this) — keep as is.
2) Get paged rows (current logic) — keep as is.
3) Get facet aggregations — new statement that repeats the same WHERE clause.

Facet aggregations SQL (conceptual):

```
-- Parameters: @q, @lat, @lon, @openNow, @cuisines text[], @tags text[], @priceBands smallint[], @topN int
WITH base AS (
  SELECT s.*
  FROM "SearchIndexItems" s
  WHERE s."SoftDeleted" = FALSE
    /* text filter: prefix for very short, else tsquery + ilike fallback */
    AND ( /* same text predicates as handler’s base filter */ )
    AND ( @openNow IS DISTINCT FROM TRUE OR (s."IsOpenNow" AND s."IsAcceptingOrders") )
    AND ( @cuisines IS NULL OR s."Cuisine" = ANY(@cuisines) )
    AND ( @priceBands IS NULL OR s."PriceBand" = ANY(@priceBands) )
    AND ( @tags IS NULL OR s."Tags" && @tags )
)
SELECT
  -- Cuisine facet (top-N, case-insensitive, non-null/non-empty)
  (
    SELECT json_agg(x) FROM (
      SELECT LOWER(c) AS value, COUNT(*) AS count
      FROM base b
      CROSS JOIN LATERAL NULLIF(b."Cuisine", '') c
      GROUP BY LOWER(c)
      ORDER BY COUNT(*) DESC, LOWER(c) ASC
      LIMIT @topN
    ) x
  ) AS cuisines,
  -- Tags facet (top-N)
  (
    SELECT json_agg(x) FROM (
      SELECT LOWER(t) AS value, COUNT(*) AS count
      FROM base b,
           LATERAL unnest(b."Tags") t
      WHERE NULLIF(t, '') IS NOT NULL
      GROUP BY LOWER(t)
      ORDER BY COUNT(*) DESC, LOWER(t) ASC
      LIMIT @topN
    ) x
  ) AS tags,
  -- Price bands facet
  (
    SELECT json_agg(x) FROM (
      SELECT b."PriceBand" AS value, COUNT(*) AS count
      FROM base b
      WHERE b."PriceBand" IS NOT NULL
      GROUP BY b."PriceBand"
      ORDER BY b."PriceBand" ASC
    ) x
  ) AS price_bands,
  -- Open-now count
  (
    SELECT COUNT(*) FROM base b
    WHERE b."IsOpenNow" AND b."IsAcceptingOrders"
  ) AS open_now_count;
```

Implementation notes:
- Keep the existing result SELECT and ORDER BY/score logic intact for paging.
- Share the same parameter bag across the count/page/facets statements to avoid drift.
- Top‑N default for `Cuisines`/`Tags`: 10 (configurable).
- Case normalization is handled by `LOWER(...)` in aggregation; UI can prettify labels if needed.

---

### Handler Changes
- Update `UniversalSearchQuery` request to include `Tags` and `PriceBands` and optional `IncludeFacets`.
- Introduce a new response DTO `UniversalSearchResponseDto` (see API Changes) and update the handler return type.
- Build the WHERE predicates once (as done today) and append new filters:
  - `Tags`: `s."Tags" && @tags` (OR semantics within the tag dimension)
  - `PriceBands`: `s."PriceBand" = ANY(@priceBands)`
- Execute three statements in one roundtrip using `QueryMultipleAsync`:
  - Pagination `COUNT(*)` SQL (existing `DapperPagination.BuildPagedSql` output’s countSql)
  - Page SQL (existing)
  - Facets SQL (new)
- Map facet JSON arrays to `List<FacetCount<T>>` via Dapper’s dynamic mapping or strongly typed records. Alternatively, select as rows (value, count) and map directly to lists — both are fine.
- Default `IncludeFacets=true` in the endpoint; allow turning off for low‑latency scenarios.

---

### Database & Migrations
No schema changes required for facets. Add helpful indexes for equality/grouping to keep p95 within target:

1) Expression index for case-insensitive cuisine equality/grouping:
- Migration SQL:
  - `CREATE INDEX IF NOT EXISTS "SIDX_Lower_Cuisine" ON "SearchIndexItems" (LOWER("Cuisine"));`
- EF: use `HasIndex(e => e.Cuisine).HasDatabaseName("SIDX_Lower_Cuisine").HasAnnotation("Npgsql:IndexExpression", "LOWER(\"Cuisine\")");` or define via `HasIndex().HasDatabaseName(...);` with a model builder `HasAnnotation`/raw migration for expression — simplest is adding via raw SQL in a migration.

2) Btree index for `PriceBand`:
- `CREATE INDEX IF NOT EXISTS "SIDX_PriceBand" ON "SearchIndexItems" ("PriceBand");`

3) Optional: composite filter helpers if needed later (e.g., partial indexes), but defer until profiling indicates necessity.

Keep existing GIN (tags) and GiST (geo) indexes as-is.

---

### Read Model Enhancements (optional but recommended)
- Price bands population (restaurants):
  - Short term: allow tests to set `PriceBand` via direct update (as done for `IsOpenNow`).
  - Longer term: compute restaurant `PriceBand` from menu price distribution (e.g., median) in `SearchIndexMaintainer` by querying `MenuItems` and binning into 1–4 bands.

- Tags population:
  - If a restaurant–tag assignment exists, enrich `SearchIndexMaintainer.UpsertRestaurantByIdAsync` to populate `Tags` and copy them into `Keywords` as well for recall.
  - If assignment is not yet modeled, keep `Tags` facet dormant; it will surface once data flows.

- Open‑now correctness:
  - Current maintainer sets `IsOpenNow=false` placeholder. Consider adding a simple evaluator from `BusinessHours` to set real‐time `IsOpenNow` periodically or on read (defer if out of scope for Phase 1).

---

### Implementation Steps (engineering TODOs)
1) Request/Response contracts
- Add `Tags`, `PriceBands`, `IncludeFacets` to `UniversalSearchQuery`.
- Add `UniversalSearchResponseDto`, `FacetBlock`, and `FacetCount<T>` types.

2) Handler
- Refactor `UniversalSearchQueryHandler` to return `UniversalSearchResponseDto`.
- Build unified WHERE (reuse current predicates) + add tags/price bands filters.
- Keep current count/page logic. Add facet SQL and fetch via `QueryMultipleAsync`.
- Wire `IncludeFacets` to optionally skip facet query.

3) EF Migration (indexes)
- Add a migration that creates:
  - `SIDX_Lower_Cuisine` expression index
  - `SIDX_PriceBand` btree index
- Keep migrations idempotent (`CREATE INDEX IF NOT EXISTS ...`).

4) Maintainer (optional Phase 1, or Phase 1.5)
- Add `PriceBand` computation stub (configurable binning; off by default) and pass through if provided by upstream.
- Add `Tags` mapping if tag assignments exist; otherwise leave as-is.

5) Web endpoint and DI
- Update the search endpoint mapping to reflect new response shape.
- Consider adding `topN` knob via configuration for facet bucket sizes.

6) Tests (functional)
- Add tests to `tests/Application.FunctionalTests/Features/Search/UniversalSearchTests.cs`:
  - `Facets_ShouldReturnTopCuisines_ForFilteredSet`
  - `Facets_ShouldRespectCuisineFilter_WhenApplied`
  - `Facets_ShouldReportOpenNowCount`
  - `Facets_ShouldReturnTags_WhenTagsPresent` (prepare data by updating read model `Tags` after upsert)
  - `Facets_ShouldReturnPriceBands_WhenPresent` (prepare data by updating `PriceBand` after upsert)
- Adjust existing tests to new response type (`Page` nested) or gate facets behind `IncludeFacets` until UI aligns.

7) Performance validation
- Seed ~5–10k rows locally; measure p95 for search+facets under typical predicates.
- If needed, reduce top‑N or add basic caching for frequent queries.

---

### Example Handler Changes (sketch)

````csharp
// Request extension
public sealed record UniversalSearchQuery(
    string? Term,
    double? Latitude,
    double? Longitude,
    bool? OpenNow,
    string[]? Cuisines,
    string[]? Tags,
    short[]? PriceBands,
    bool IncludeFacets,
    int PageNumber,
    int PageSize
);

public sealed record UniversalSearchResponseDto(
    PaginatedList<SearchResultDto> Page,
    FacetBlock Facets
);

public sealed record FacetBlock(
    IReadOnlyList<FacetCount<string>> Cuisines,
    IReadOnlyList<FacetCount<string>> Tags,
    IReadOnlyList<FacetCount<short>> PriceBands,
    int OpenNowCount
);

public sealed record FacetCount<T>(T Value, int Count);

// WHERE fragments additions in handler
if (request.Tags is { Length: > 0 }) { where.Add("s.\"Tags\" && @tags"); p.Add("tags", request.Tags); }
if (request.PriceBands is { Length: > 0 }) { where.Add("s.\"PriceBand\" = ANY(@priceBands)"); p.Add("priceBands", request.PriceBands); }

// Facets SQL executed with the same parameters (plus @topN)
````

---

### Risks & Mitigations
- Breaking response shape: mitigate by adding `IncludeFacets` flag or updating caller + tests in the same change.
- Tags facet empty due to missing data: acceptable for Phase 1; plan maintainer enrichment.
- Performance of repeated WHERE in multiple statements: acceptable for Phase 1; can consolidate with a temp table or materialized CTE later if necessary.
- Case consistency for cuisine/tags: normalize to lower in aggregation; UI prettifies labels if needed.

---

### Success Criteria
- Facet counts reflect the current filters and update when filters change.
- Top‑N cuisine/tags returned deterministically with stable ordering.
- p95 latency remains within target on typical datasets.
- Tests cover facet correctness and interaction with filters.

---

### Follow‑ups (beyond Phase 1)
- Populate `PriceBand` and `Tags` via maintainer from canonical sources.
- Consider disjunctive faceting (counts excluding the facet’s own selection) if UX needs it.
- Cache facet responses for very hot, repeated queries.
- Add UI “chips” with counts and an “open‑now” toggle wired to the API.

