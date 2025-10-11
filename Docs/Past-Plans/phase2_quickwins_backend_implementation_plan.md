# Phase 2 Quick Wins – Backend Implementation Plan

Scope: Minimal, non-breaking enhancements to improve discovery UX without adding new endpoints.
Date: 2025-10-10

## Overview (Targets)
- Autocomplete: add `limit` (default 10).
- Restaurants search: support `lat`, `lon`, `sort=rating|distance`, and return `distanceKm` when geo present.
- Restaurant info: document optional `avgRating`, `ratingCount` on `/restaurants/{id}/info`.

## 1) Autocomplete `limit` (default 10)
Files to change
- `src/Web/Endpoints/Search.cs`
- `src/Application/Search/Queries/Autocomplete/AutocompleteQueryHandler.cs`
- `src/Application/Search/Queries/Autocomplete/AutocompleteQueryHandler.cs` (validator section)

Steps
1. Web DTO: extend `AutocompleteRequestDto` with `int? Limit`.
2. Web endpoint: coalesce `var limit = req.Limit ?? 10;` and pass into the query.
3. App query: change to `record AutocompleteQuery(string Term, int Limit)`.
4. Validator: add `RuleFor(x => x.Limit).InclusiveBetween(1, 50)` with defaulted value supplied by endpoint.
5. SQL: replace hard-coded `LIMIT 10` with `LIMIT @limit` and pass parameter.
6. OpenAPI: update summary/description to mention `limit` and default of 10.

Acceptance
- When `limit` omitted, API returns max 10 suggestions.
- When `limit=3`, API returns ≤ 3 items; validation rejects values outside 1..50 with RFC7807 error.

## 2) Restaurants search: geo + sort + distanceKm
Files to change
- `src/Web/Endpoints/Restaurants.cs`
- `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQuery.cs`
- `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQueryHandler.cs`
- `src/Application/Restaurants/Queries/Common/RestaurantDtos.cs`

Steps
1. Web endpoint signature: add `string? sort` (values: `rating`, `distance`). Keep existing `lat`, `lng`.
2. Query contract: extend `SearchRestaurantsQuery` with `string? Sort` (or small enum mapped from string).
3. SQL select: include computed `DistanceKm` when `lat/lng` provided, else `NULL AS DistanceKm`.
   - Haversine in Postgres (km):
     ```sql
     CASE WHEN @Lat IS NOT NULL AND @Lng IS NOT NULL AND r."Geo_Latitude" IS NOT NULL AND r."Geo_Longitude" IS NOT NULL THEN
       6371 * 2 * ASIN(SQRT(POWER(SIN(RADIANS((@Lat - r."Geo_Latitude")/2)),2) + COS(RADIANS(@Lat)) * COS(RADIANS(r."Geo_Latitude")) * POWER(SIN(RADIANS((@Lng - r."Geo_Longitude")/2)),2)))
     ELSE NULL END AS DistanceKm
     ```
4. OrderBy logic:
   - Default: keep stable order (name asc, id asc).
   - `sort=rating`: `ORDER BY COALESCE(rr."AverageRating",0) DESC NULLS LAST, r."Name" ASC`.
   - `sort=distance`: when `lat/lng` present `ORDER BY DistanceKm ASC NULLS LAST, r."Name" ASC`; if absent, ignore and fall back to default.
5. DTO: add nullable `decimal? DistanceKm` to `RestaurantSearchResultDto` (last parameter to avoid widespread changes) and map in handler.
6. Response docs: update `.WithDescription` to mention `sort=rating|distance` and `distanceKm` presence when geo supplied.

Acceptance
- `GET /restaurants/search?lat=...&lng=...&sort=distance` returns items ordered by `distanceKm` ascending with `distanceKm` populated.
- `GET /restaurants/search?sort=rating` orders by rating desc; ties by name.
- If `sort=distance` without `lat/lng`, endpoint still works and uses default sort; `distanceKm` is null.

## 3) Restaurant info: optional rating fields (documentation-first)
Files to change
- `src/Web/Endpoints/Restaurants.cs`
- `src/Web/Contracts/Restaurants/PublicInfoResponseDto.cs` (new)
- Docs under `Docs/API-Documentation` (follow project structure; if absent, add a short addendum in `Docs/Architecture` for now)

Steps
1. Add a Web response DTO: `RestaurantPublicInfoResponseDto` mirroring `RestaurantPublicInfoDto` plus nullable `decimal? AvgRating`, `int? RatingCount`.
2. Map in endpoint: adapt result to new response type; set new fields to `null` (no backend join required for MVP).
3. Update endpoint metadata: `.Produces<RestaurantPublicInfoResponseDto>(200)` and description noting optional fields.
4. Documentation: note these fields are optional, may be null, and will be populated once review summary wiring is added; mark as non-breaking.

Acceptance
- OpenAPI shows `avgRating` and `ratingCount` as nullable fields on `/restaurants/{id}/info` response.
- Existing clients remain unaffected; new clients can read fields when present.

## Cross-Cutting Tasks
- Validation and defaults
  - Keep Minimal API parameter binding permissive; coalesce to defaults inside handlers to avoid early 400s.
- Contract tests (additive)
  - `tests/Web.ApiContractTests`:
    - Autocomplete returns max N and honors `limit`.
    - Restaurants search returns `distanceKm` only when geo present; validates ordering semantics for `distance` and `rating`.
    - Restaurant info schema includes optional `avgRating`, `ratingCount`.
- Docs
  - Update route docs and examples; reflect defaults and parameter ranges.

## Estimated Effort
- Autocomplete `limit`: S (≤ 1 dev-hour)
- Restaurants search distance/sort/field: M (0.5–1 day including SQL + tests)
- Restaurant info optional fields (contract + docs): S (≤ 2 dev-hours)

## Rollout & Safety
- Backward compatible: all changes are additive.
- Feature flags not required; behavior gated by query params and nullability.
- DB migrations: none.

## Implementation Order
1) Autocomplete limit (lowest risk, fast).
2) Restaurants search distance/sort (core user impact).
3) Restaurant info optional fields (contract + docs).

