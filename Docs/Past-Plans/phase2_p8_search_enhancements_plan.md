# Phase 2 – P8 Search Enhancements: Implementation Plan

Date: 2025-10-10
Scope: Extend public search for better discovery while staying backward compatible.

Goals
- Reduce client logic by adding server-side filters/sorts and map support.
- Keep responses compact via projections when desired.
- Avoid schema or storage migrations where possible.

Out of scope (for now)
- Full text re-ranking beyond current strategy.
- CMS-driven facets or synonyms.
- BBox rate limiting and map tile density controls.

---

API Changes (additive)
1) GET /api/v1/search (Universal Search)
- New query params:
  - `entityTypes`: array of `Restaurant|MenuItem|Tag` (filters by `SearchIndexItems.Type`).
  - `sort`: `relevance|distance|rating|priceBand|popularity`.
  - `bbox`: `minLon,minLat,maxLon,maxLat` (filters by viewport using PostGIS).
  - `fields`: `light|default|full` (projection hint).
- Behavior:
  - `sort=distance` requires `lat` and `lon`; otherwise ignored (fallback to relevance).
  - `rating` sorts by `AvgRating` desc, ties by `ReviewCount` then `UpdatedAt`.
  - `priceBand` sorts by `PriceBand` asc (low to high).
  - `popularity` sorts by `ReviewCount` desc, ties by `AvgRating`.
  - `bbox` filters to items whose `Geo` intersects the envelope (SRID 4326).
  - `fields=light` omits `badges` and `reason`, and does not calculate them; `default` = current; `full` reserved.

2) GET /api/v1/restaurants/search
- New/extended query params:
  - `openNow`: boolean (filters to accepting/open restaurants when true).
  - `priceBand`: number (1–4); optional array support later.
  - `bbox`: `minLon,minLat,maxLon,maxLat` (same semantics as above).
  - `sort`: add `popularity` to existing `rating|distance`.
- Response:
  - Already returns `distanceKm` when geo present (done).

3) GET /api/v1/search/autocomplete
- New query param:
  - `types`: array filter on `Restaurant|MenuItem|Tag` (optional). Default: all types.
- Existing `limit` retained (done).

---

Design & Implementation Steps

Universal Search
1. Request/DTO updates
- File: `src/Web/Endpoints/Search.cs` → extend `UniversalSearchRequestDto` with `string[]? EntityTypes`, `string? Sort`, `string? Bbox`, `string? Fields`.
- File: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs`
  - Extend query record or add a wrapper to carry new params.
  - Validator (new) to enforce: sort in allowed set; bbox format and bounds; fields in allowed set; entity types mapped to stored labels.

2. SQL changes (Dapper)
- WHERE: add `s."Type" = ANY(@types)` when `entityTypes` provided.
- WHERE: add bbox filtering when `bbox` provided, using:
  - `s."Geo" IS NOT NULL AND ST_Intersects(s."Geo", ST_Envelope(ST_GeomFromText('POLYGON((minLon minLat, minLon maxLat, maxLon maxLat, maxLon minLat, minLon minLat))', 4326)))` or simpler: `ST_MakeEnvelope(minLon,minLat,maxLon,maxLat,4326)::geography` with `ST_Intersects(s."Geo", ...)`.
- ORDER BY: build per sort key; keep stable tie-breakers (`UpdatedAt`, `Id`).
- Projection: when `fields=light`, skip badges/reason computation and project `badges=[]`, `reason=null`.

3. Endpoint behavior
- Coalesce defaults after model binding (avoid Minimal API early 400s).
- Backward compatibility: if any new param invalid, return validation problem; otherwise ignore unknown values.

Restaurants Search
1. Request/DTO updates
- File: `src/Web/Endpoints/Restaurants.cs` → extend `/search` signature with `bool? openNow`, `short? priceBand`, `string? bbox`, keep `sort`.
- File: `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQuery.cs` → add properties: `bool? OpenNow`, `short? PriceBand`, `string? Bbox`.
- File: `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQueryValidator.cs` →
  - Validate `priceBand` ∈ [1..4].
  - Validate `bbox` format and numeric bounds.
  - Permit `openNow` when true.

2. SQL changes (Dapper)
- WHERE: add `r."IsAcceptingOrders" = TRUE` (and hours check when available) for `openNow`.
- WHERE: add `r."Geo_Latitude"/"Geo_Longitude"` within bbox using the same envelope test.
- ORDER BY: add `popularity` → `COALESCE(rr."TotalReviews",0) DESC, COALESCE(rr."AverageRating",0) DESC`.

3. Response
- Already includes `distanceKm` (done). No schema change needed.

Autocomplete
1. Request/DTO updates
- File: `src/Web/Endpoints/Search.cs` → extend `AutocompleteRequestDto` with `string[]? Types`.
- File: `src/Application/Search/Queries/Autocomplete/AutocompleteQueryHandler.cs`
  - Change query to carry `Types` (optional).
  - WHERE: add `s."Type" = ANY(@types)` when provided.
  - Keep `limit` enforcement and validation (done).

---

Validation Rules
- `sort` (universal): allowed `relevance|distance|rating|priceBand|popularity`; soft-ignore invalid by falling back to `relevance` (contract can still 400 if we prefer strictness).
- `sort` (restaurants): allowed `distance|rating|popularity`; distance requires lat/lng.
- `bbox`: must be `minLon,minLat,maxLon,maxLat` with min < max; lon ∈ [-180,180], lat ∈ [-90,90].
- `fields`: `light|default|full` (treat unrecognized as `default`).
- `entityTypes`/`types`: map case-insensitively to stored labels.

Performance & Indexing
- Existing: `SearchIndexItems.Geo` GIST index (SIDX_Geo) is present; supports bbox intersects.
- Add (if missing): btree indexes on `SearchIndexItems.Type`, `SearchIndexItems.PriceBand`, and restaurant review summaries (`AverageRating`, `TotalReviews`).
- Keep pagination limits (max pageSize 50) to protect from expensive sorts.

Caching & Responses
- Maintain current behavior; consider adding short OutputCache for `/restaurants/search` and `/search` later.
- Projections reduce CPU (skip badge/reason calc) when `fields=light`.

Testing Plan
- Contract tests (Web.ApiContractTests):
  - `/search`: entityTypes filter mapping; bbox filter mapping; sort → correct ordering; fields=light omits badges/reason.
  - `/restaurants/search`: openNow, priceBand, bbox mapping; popularity sort ordering.
  - `/search/autocomplete`: types filter mapping; limit honored.
- Functional tests (Application.FunctionalTests):
  - Seed SearchIndexItems/Restaurants with known coords/ratings; verify ordering and bbox filtering.

Docs Plan
- Update `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md`:
  - `/search`: add `entityTypes`, `sort`, `bbox`, `fields`.
  - `/restaurants/search`: add `openNow`, `priceBand`, `bbox`, `popularity` sort.
  - `/search/autocomplete`: add `types`.

Risks
- Bbox can increase QPS from map scrubbing; mitigate via pageSize cap and future OutputCache.
- Popularity sort could skew toward long-standing restaurants; consider time-decay in fast-follow if needed.

Phasing (suggested)
1) Autocomplete `types` (S) and `/restaurants/search` `popularity` sort (S).
2) `/search` entityTypes + sorts (M).
3) bbox for both endpoints (M).
4) `fields` projection for `/search` badges/reason (S–M).

Estimates
- S ≈ 0.5–1 day; M ≈ 1–3 days. Total: ~5–8 dev-days including tests/docs.

