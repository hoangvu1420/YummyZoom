# Search Feature – Detailed Backend Implementation Plan (P1–P6)

Date: 2025-10-18
Owner: Backend team
Scope: Implement decisions captured in 181025_search_backend_gap_analysis.md, following current architecture (Domain → Application → Infrastructure → Web) and test strategy.

## Conventions and Patterns
- Queries/DTOs in `src/Application` with validators; use Dapper for read-side aggregation.
- Minimal APIs in `src/Web/Endpoints` returning `Result` via `.ToIResult()` helpers; keep public endpoints thin.
- Read models and SQL in `Infrastructure` for performance-sensitive discovery/search paths.
- Contract tests in `tests/Web.ApiContractTests`; functional tests in `tests/Application.FunctionalTests` when behavior spans multiple layers.
- Backward compatibility preserved; new features are opt-in via query flags or feature toggles.

---

## P1 – Promotions Row: Active Deals (Decision: Option B)
Expose a public endpoint surfacing restaurants that have currently active coupons, with a best-available coupon label.

### Deliverables
- `GET /api/v1/home/active-deals?limit=10`
- Response items: `{ restaurantId, name, logoUrl, bestCouponLabel }`

### Steps
1) Application (Query + DTO)
- Add `src/Application/Home/Queries/ActiveDeals/ListActiveDealsQuery.cs`
  - `record ListActiveDealsQuery(int Limit)` → `Result<IReadOnlyList<ActiveDealCardDto>>`
  - `record ActiveDealCardDto(Guid RestaurantId, string Name, string? LogoUrl, string BestCouponLabel)`
  - Validator: `Limit` in [1..50], default 10.
2) Application (Handler)
- Add Dapper-based handler in `src/Application/Home/Queries/ActiveDeals/ListActiveDealsQueryHandler.cs` that:
  - Selects restaurants that have at least one enabled coupon with `ValidityStartDate <= now <= ValidityEndDate`.
  - Picks a label per restaurant using a cheap heuristic (prefer Percentage, else FixedAmount, else FreeItem), e.g. `CASE`-based selection.
  - Orders by (has percentage desc, percentage desc, fixed amount desc, reviewCount desc).
3) Web Endpoint
- New endpoint group `src/Web/Endpoints/Home.cs` with `publicGroup.MapGet("/active-deals", ...)` → send `ListActiveDealsQuery`.
- `.WithName("Home_ActiveDeals")` + summary/description; `Produces<IReadOnlyList<ActiveDealCardDto>>()`.
5) Tests
- Web.ApiContractTests: `HomeActiveDealsContractTests` asserting shape, limit semantics, deterministic ordering with seeded data.
- Functional test with seeded coupons to ensure label heuristic.
6) Docs
- Update `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md` Active Deals section.

---

## P2 – DiscountedOnly Filter (Restaurants Search)
Add `discountedOnly=true` filter to `/api/v1/restaurants/search`.

### Deliverables
- Support `discountedOnly` query flag in endpoint; results restricted to restaurants with active coupon(s).

### Steps
1) Application (Query contract)
- Update `SearchRestaurantsQuery` to add `bool? DiscountedOnly` (nullable for backward compatibility).
- Update validator if present; keep default off.
2) Application (Handler SQL)
- In `SearchRestaurantsQueryHandler`, add `EXISTS` predicate when `DiscountedOnly == true`:
  - `EXISTS (SELECT 1 FROM "Coupons" c WHERE c."RestaurantId" = r."Id" AND c."IsEnabled" = TRUE AND now() BETWEEN c."ValidityStartDate" AND c."ValidityEndDate")`
- Ensure parameters and combined filters still leverage indexes.
3) Web Endpoint
- In `src/Web/Endpoints/Restaurants.cs` search route, add `bool? discountedOnly` arg; pass through to query.
4) Tests
- Contract test: `/restaurants/search?discountedOnly=true` returns only discounted restaurants.
- Functional test: verify interaction with other filters (e.g., minRating, bbox) still works.
5) Docs
- Update restaurant search docs with `discountedOnly` flag.

---

## P3 – Facets for /restaurants/search
Add `includeFacets` to return cuisine/tag/priceBand buckets and openNowCount similar to `/search`.

### Deliverables
- `GET /api/v1/restaurants/search?...&includeFacets=true` returns `{ page, facets }`.

### Steps
1) Application (DTO)
- New response wrapper: `record RestaurantSearchWithFacetsDto(PaginatedList<RestaurantSearchResultDto> Page, RestaurantFacetsDto Facets)`
- `record RestaurantFacetsDto(IReadOnlyList<FacetCount<string>> Cuisines, IReadOnlyList<FacetCount<string>> Tags, IReadOnlyList<FacetCount<short>> PriceBands, int OpenNowCount)`
- Reuse `FacetCount<T>` from Universal Search or define a local analog to avoid coupling.
2) Application (Handler)
- Extend `SearchRestaurantsQuery` with `bool IncludeFacets` (default false).
- When true, compute facets:
  - Cuisine: `GROUP BY LOWER(r."CuisineType")` over filtered base.
  - Tags: unnest dietary tag ids via join onto Tags; `GROUP BY LOWER(t."TagName")`.
  - PriceBand: if available via SearchIndex or a restaurant-level band; if not, return empty array (MVP).
  - openNowCount: using `Restaurants` business hours evaluator or fall back to `IsAcceptingOrders` if simple; start with `IsAcceptingOrders`.
3) Web Endpoint
- Accept `bool? includeFacets` and return:
  - When `false`/unset: the original `PaginatedList<RestaurantSearchResultDto>` (unchanged).
  - When `true`: `{ page, facets }` wrapper (endpoint already `.Produces<object>` allowing dual shape).
4) Tests
- Contract tests for both shapes; verify counts under filters and pagination.
5) Docs
- Add response examples for facet-enabled variant.

---

## P4 – Trending Searches - No Implementation
Log search terms and expose top-N in a time window.

### Deliverables
- `SearchQueryLog` table + minimal logging from search endpoints.
- `GET /api/v1/search/trending?days=7&limit=20`.

### Steps
1) Data Model (Migration)
- EF migration `20251018_AddSearchQueryLog`: table `SearchQueryLog(Id uuid pk, Term text not null, UserId uuid null, CreatedAt timestamptz not null default now())`.
- Indexes: `IX_SearchQueryLog_Term_CreatedAt`, optional partial on `CreatedAt` for recency scans.
2) Application (Logger abstraction)
- Interface `ISearchQueryLogger` in `Application.Abstractions` with `Task LogAsync(string term, Guid? userId, CancellationToken)`.
- Infra implementation using Dapper/EF to insert rows; trim/normalize terms (lowercase, length<=64).
3) Web Integration
- In `src/Web/Endpoints/Search.cs`, after successful handling of `/search` and `/autocomplete`, fire-and-forget `ISearchQueryLogger.LogAsync()` for non-empty terms; ignore errors.
4) Application (Query + Handler)
- Add `ListTrendingSearchesQuery(int Days, int Limit)` → `IReadOnlyList<TrendingTermDto(term, count)>` with validation.
- Handler: `SELECT term, COUNT(*) FROM SearchQueryLog WHERE CreatedAt >= now() - interval '@Days days' GROUP BY term ORDER BY COUNT(*) DESC, term ASC LIMIT @Limit`.
5) Web Endpoint
- Add `publicGroup.MapGet("/trending", ...)` in `Search` endpoint group.
6) Tests
- Contract tests for shape and ordering; functional test to assert logging + retrieval.
7) Docs
- Add “Trending” section under Search.

---

## P5 – ETA/Fee Preview (Feature-flagged Phase 1) - No Implementation
Compute heuristic ETA and delivery fee for results when geo is provided.

### Deliverables
- Config-gated estimates included in result payloads (either as extra fields or badges data).

### Steps
1) Configuration
- Add `Search:Estimates:Enabled` + simple coefficients in `appsettings` (e.g., `BasePrepMinutes`, `PerKmMinutes`, `BaseFee`, `PerKmFee`).
2) Application (Abstraction)
- Interface `IDeliveryEstimateService` with `Estimate(lat, lon, restLat, restLon) → (etaMinutes?, fee?)`.
3) Infrastructure (Implementation)
- `Infrastructure/Services/DeliveryEstimateService.cs` implementing haversine distance → ETA/fee heuristics.
4) Application/Web (Projection)
- Option A (non-breaking): include an additional badge `{ code: "eta_fee", data: { etaMinutes, estimatedFee } }` in Universal Search mapping when enabled and coordinates exist.
- Option B (typed): add nullable fields to `RestaurantSearchResultDto` and `SearchResultDto`. If chosen, ensure clients tolerate nulls; update docs.
- Start with Option A (badge) to avoid DTO churn.
5) Tests
- Unit test distance/fee math; contract test asserting badge presence only when feature enabled and lat/lon provided.
6) Docs
- Document estimates as experimental/approximate and feature-gated.

---

## P6 – Restaurant Details Bootstrap - No Implementation
Aggregate info + review summary + menu meta in one call.

### Deliverables
- `GET /api/v1/restaurants/{id}/details?include=info,reviewSummary,menuMeta`

### Steps
1) Application (Query + DTO)
- `GetRestaurantAggregatedDetailsQuery(Guid RestaurantId, bool IncludeInfo, bool IncludeReviewSummary, bool IncludeMenuMeta)`
- DTO: `RestaurantAggregatedDetailsDto(RestaurantPublicInfoResponseDto? Info, RestaurantReviewSummaryDto? ReviewSummary, MenuMetaDto? MenuMeta)` where `MenuMetaDto(IReadOnlyList<MenuCategoryMetaDto> Categories, int TotalItems)`.
2) Application (Handler)
- Compose: reuse existing queries `GetRestaurantPublicInfoQuery` and `GetRestaurantReviewSummaryQuery`; for menu meta, query `FullMenuView` (categories and counts) or a light Dapper aggregation on `MenuItems` grouped by category.
3) Web Endpoint
- Add in `Restaurants` public group: parse `include` tokens; invoke new query; return aggregated DTO.
4) Tests
- Contract test verifies presence/absence based on `include` flags; functional test over seeded data asserts counts.
5) Docs
- Add endpoint spec with example payload and `include` semantics.

---

## Cross‑Cutting Tasks
- Add/adjust EF Core migrations for P1 index and P4 log table.
- Update `Docs/API-Documentation/...` with new/changed endpoints: Active Deals, Restaurant Search facets, DiscountedOnly, Trending, Details Bootstrap, and (if exposed) estimates badge description.
- Add Web.ApiContractTests for each new endpoint/variant.
- Seed data adjustments (optional) to exercise discounts and trends in dev.

## Rollout Plan (by iterations)
- Iteration 1: P1 (Active Deals), P3 (Facets)
- Iteration 2: P4 (Trending), P6 (Details Bootstrap)
- Iteration 3: P2 (DiscountedOnly), P5 (ETA/Fee phase 1, behind flag)

## Acceptance Checklists
- P1: returns real discount labels; respects limit; <150ms on dev.
- P2: discountedOnly returns subset; combines with minRating/bbox; perf sane.
- P3: facets match filtered set; shape compatible when `includeFacets=false`.
- P4: terms logged (no PII beyond optional userId); trending respects window/limit.
- P5: estimates appear only when enabled and lat/lon present; reasonable values.
- P6: one round trip provides all requested blocks; fields match existing DTOs.

---

Notes:
- Keep SQL parameterized; prefer `QueryPageAsync` helpers for pagination.
- Avoid long-running facet queries by limiting buckets (top-N) and pushing non-essential facets later if perf regresses.

