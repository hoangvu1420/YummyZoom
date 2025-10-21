# Search Feature – Backend Gap Analysis and Plan

Date: 2025-10-18
Audience: Backend/API and Mobile/Web teams

## Executive Summary

The current backend already covers the MVP flows the frontend outlined for search entry, typing/autocomplete, and restaurant-focused results:

- Universal search with facets and sorting exists: `GET /api/v1/search` with `includeFacets`, `entityTypes`, geo, and multiple sorts (relevance, distance, rating, priceBand, popularity). See src/Web/Endpoints/Search.cs and src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs.
- Autocomplete exists: `GET /api/v1/search/autocomplete` with optional type filters. See src/Web/Endpoints/Search.cs and src/Application/Search/Queries/Autocomplete/AutocompleteQueryHandler.cs.
- Restaurant search exists: `GET /api/v1/restaurants/search` with `q`, `cuisine`, `tags`/`tagIds`, `minRating`, `bbox`, geo distance and sorts (rating|distance|popularity). See src/Web/Endpoints/Restaurants.cs and src/Application/Restaurants/Queries/SearchRestaurants.
- Tags “Top” exists for discovery chips: `GET /api/v1/tags/top`. See src/Web/Endpoints/Tags.cs and src/Application/Tags/Queries/ListTopTags.
- “Recently ordered” exists and is auth-scoped: `GET /api/v1/orders/my`. See src/Web/Endpoints/Orders.cs.

What’s missing are optional enhancements the frontend would like post-MVP: promotions/banners, discount/loyalty filters, ETA/fee previews in results, trending searches/personalization, and facets for the dedicated `/restaurants/search` endpoint. Below we map each gap to current capabilities, a recommended backend approach, and workload.

## Current Building Blocks (backend)

- Read models and maintenance
  - Search index: SearchIndexItems maintained via SearchIndexMaintainer; supports open-now, accepting-orders, rating counts, price band, and geo. See src/Infrastructure/Persistence/ReadModels/Search/...
  - Menu read model: FullMenuView (+ caching headers). See src/Infrastructure/Persistence/ReadModels/FullMenu/...
  - Review summaries: RestaurantReviewSummary. See src/Infrastructure/Persistence/ReadModels/Reviews/...
  - Menu item popularity and sales summaries (basis for “popular” feeds). See src/Infrastructure/Persistence/ReadModels/MenuItemSales/...
- Endpoints already exposed
  - Search, Autocomplete, Tags Top, Menu Items Feed, Restaurant public info/menu/reviews, Orders (including recent orders).
- Domains/commands present
  - Coupons domain (CRUD + fast-check for carts), Orders lifecycle, Menus/Items management, Reviews.

These provide enough substrate to implement most of the requested enhancements without new infrastructure beyond a small aggregation or analytics table for “trending terms.”

## Gap-by-Gap Feasibility and Proposed Implementations

For priorities referenced by the frontend, we align them to P1–P6 as inferred from their plan.

### P1 – Promotions/Featured Row (home/search entry) 

Frontend need: a “promotions/featured” row on the entry screen. No current `/promotions` or `/home` aggregator.

Backend options:
- Option A (fastest): “Featured Picks” endpoint that surfaces restaurants ranked by signal mix (recent popularity, rating, open now). Can reuse existing menu-item popularity + review summary data. Endpoint: `GET /api/v1/home/featured?limit=10` returning a lightweight restaurant card DTO.
- Option B (coupons-aware): “Active Deals” endpoint that surfaces restaurants with active coupons, optionally attaching the best coupon label (from Coupons domain). Endpoint: `GET /api/v1/home/active-deals?limit=10`. Requires a simple EXISTS join over active coupons or a tiny read model for performance.

Recommendation: Ship Option A for immediate parity (“Featured Picks”). Plan Option B as a follow-up if we want the label “deal” to reflect real discounts.

Effort: Option A low (1–2 days: query + DTO + tests). Option B medium (3–4 days: query against Coupons + best-deal computation + tests). Operational risk: low.

Decision: Implement Option B as it provides more value to users by highlighting actual discounts.

### P2 – Discount/Loyalty Filters in Search 

Frontend need: filters for “discounted only” and “loyalty/bePoint.”

Backend state: Coupons domain exists and can evaluate deals for a cart; no loyalty program entities exist; search index does not carry “hasActiveCoupon”/loyalty flags.

Backend approach:
- “Discounted only” (restaurants): add optional filter `discountedOnly=true` to `GET /api/v1/restaurants/search`, implemented via EXISTS on Coupons table with validity window. For universal `/search`, we can gate this to Restaurant-only entity type or ignore when `entityTypes` excludes restaurants.
- “Loyalty/bePoint”: deferred. Requires introducing a loyalty domain (points accrual, merchant opt-in), which is out of MVP scope.

Effort: discountedOnly = medium (2–3 days including tests). Loyalty = high (multi-sprint; new domain).

Risk/perf: moderate. Add a partial index on coupons by validity and restaurant to keep the EXISTS filter cheap.

Decision: There is no loyalty program yet, skip this for now. Implement discountedOnly filter.

### P3 – Facets for Dedicated Restaurant Search 

Frontend need: facet chips (cuisines, tags, price bands) from `/restaurants/search` (not only from `/search`).

Backend state: `/search` already computes facets on SearchIndexItems. `/restaurants/search` is a simpler SQL query with no facets yet.

Backend approach:
- Add `includeFacets` boolean to `/api/v1/restaurants/search`. Compute facets using the same patterns we already use in UniversalSearchQueryHandler (GROUP BY cuisine/tag/priceBand over the filtered set). Return a `{ page, facets }` envelope with backward-compatible shape.

Effort: low/medium (1–2 days). Risk: low.

Decision: Implement facets on `/restaurants/search`.

### P4 – Trending Searches (server-side)

Frontend need: server-provided trending terms for entry suggestions and smarter placeholders.

Backend approach (lean analytics):
- New table `SearchQueryLog(term TEXT, user_id UUID NULL, created_at TIMESTAMPTZ)`
- Lightweight logger in `/search` and `/search/autocomplete` handlers (debounced client will naturally limit volume) that enqueues or directly inserts normalized terms (length, alpha/num filter).
- Aggregation endpoint `GET /api/v1/search/trending?days=7&limit=20` that returns top-N terms in a window. For scale, a nightly rollup into `SearchTrendsDaily(term, date, count)` can be added later.

Effort: medium (2–3 days: migration + logging + endpoint + tests). Risk: low; mindful of PII and rate limiting.

Decision: No implementation; frontend can mock the trends for MVP. 

### P5 – ETA/Fee Preview in Results

Frontend need: ETA and delivery fee displayed on result tiles.

Backend state: ETA is determined during order acceptance; fee exists on orders and in team cart flows, but no generalized estimate service. No per-restaurant SLAs or fee tables exposed in discovery.

Backend approach (phased):
- Phase 1 heuristic service (backend-only): `DeliveryEstimateService` that computes ETA as `prepBaselineByCuisineOrRestaurant + transit(distance)` and fee via a simple rule (e.g., base + per-km). Add optional query params (`lat/lon`) to get per-result `etaMinutes` and `estimatedFee`.
- Phase 2: persist per-restaurant configuration and historical SLA metrics; move heuristics to a read model.

Effort: high (5–8 days for Phase 1 including config, query shaping, and tests). Risk: medium (UX sensitivity to accuracy; must gate behind feature flag and omit if inputs insufficient).

Decision: Defer for now; focus on other priorities.

### P6 – Aggregated Details Bootstrap

Frontend need: reduce round-trips on Restaurant Details load (info + review summary + menu meta).

Backend state: `GET /restaurants/{id}/info` already inlines review summary; `GET /restaurants/{id}/menu` is separate and cached.

Backend approach:
- New endpoint `GET /api/v1/restaurants/{id}/details?include=info,reviewSummary,menuMeta` returning a single payload: `info` (existing), `reviewSummary` (existing), and `menuMeta` (category list + item counts). Keep full menu fetch separate for caching and large payload.

Effort: medium (2 days). Risk: low.

Decision: No implementation; frontend can continue with separate calls for MVP.

## Suggested API Additions (concrete)

1) Featured Picks
```
GET /api/v1/home/featured?limit=10
200 [{ restaurantId, name, logoUrl, avgRating, ratingCount, distanceKm? }]
```

2) Active Deals (optional follow-up)
```
GET /api/v1/home/active-deals?limit=10
200 [{ restaurantId, name, logoUrl, bestCouponLabel }]
```

3) Restaurant Search Facets
```
GET /api/v1/restaurants/search?...&includeFacets=true
200 { page: {...}, facets: { cuisines:[], tags:[], priceBands:[], openNowCount:int } }
```

4) Discounted Only filter
```
GET /api/v1/restaurants/search?...&discountedOnly=true
```

5) Trending Terms
```
GET /api/v1/search/trending?days=7&limit=20
200 [{ term, count }]
```

6) Restaurant Details Bootstrap
```
GET /api/v1/restaurants/{id}/details?include=info,reviewSummary,menuMeta
200 { info:{...}, reviewSummary:{...}, menuMeta:{ categories:[{id,name,itemCount}], totalItems } }
```

## Workload and Priority Snapshot

- P1 Featured Picks (Option A) – Low, 1–2 days. Immediate UX lift on entry.
- P3 Facets for /restaurants/search – Low/Medium, 1–2 days. Improves filtering UX.
- P4 Trending Terms – Medium, 2–3 days. Enables dynamic hints and chips.
- P6 Aggregated Details – Medium, 2 days. Faster details TTI; cleaner client code.
- P2 Discounted Only – Medium, 2–3 days. Visible value; requires careful SQL and index.
- P5 ETA/Fee Preview – High, 5–8 days (Phase 1). Gate and iterate.

Dependencies/notes:
- P2 benefits from a DB index on coupons (restaurantId, validity window, isEnabled).
- P4 requires a migration and either a background rollup or on-demand GROUP BY with a rolling time window.
- P5 should be feature-flagged and optionally driven by config per environment.

## Backward Compatibility and Contracts

- Keep existing response shapes intact. For facets on `/restaurants/search`, add `includeFacets=false` default and return the existing `page` as-is when false.
- New endpoints use dedicated names and won’t break clients. Add Web.ApiContractTests for each.

## Validation and Tests (what to cover)

- Contract tests
  - Featured Picks returns consistent shape, respects `limit`, stable ordering.
  - Restaurant Search with `includeFacets=true` returns facets with correct counts under filters.
  - Trending returns no PII and respects window/limit.
- Functional tests
  - DiscountedOnly filter returns only restaurants with active coupons in-window.
  - Details bootstrap returns info + reviewSummary + menuMeta with correct counts.
- Performance smoke
  - Facet queries bounded (top-N) and run under 100–150ms on dev data.

## Risks and Mitigations

- Promotions accuracy (Option A vs Option B): label as “Featured Picks” until coupons-aware row ships.
- ETA precision: ship behind a flag; omit fields if inputs are missing.
- Trending logging volume: minimal data, throttle at handler; add TTL/cleanup if needed.

## Proposed Sequencing (2–3 short iterations)

Iteration 1 (quick wins)
- P1 Featured Picks (A)
- P3 Restaurant facets

Iteration 2
- P4 Trending terms (log + endpoint)
- P6 Details bootstrap

Iteration 3
- P2 DiscountedOnly
- P5 ETA/Fee Preview (Phase 1, behind flag)

---

If you want, I can open small PRs per item starting with P1 and P3 to keep reviews focused and de-risked.

