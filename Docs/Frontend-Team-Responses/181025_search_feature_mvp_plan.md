# Search Feature MVP – UX Mapping, API Gaps, and Implementation Plan

Date: 2025-10-18
Scope: Customer app – Search entry, typing/autocomplete, and results.

Sources reviewed
- Core: docs/core/development_manual.md, docs/core/screens-and-pages-map.md
- Customer API: docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md, 01-Authentication-and-Profile.md, 03-Individual-Orders.md, 04-Reviews-and-Ratings.md
- Discovery notes: docs/backend-proposals/phase2_discovery_api_analysis.md, docs/features/restaurant_discovery_flow.md

## UX → API Mapping (by screen)

### 1) Search Entry (open search from Home, no term yet)
- Search bar placeholder
  - Desired: context-aware hint (e.g., “Tìm nước ép thơm”).
  - API today: no “trending searches” endpoint. Autocomplete requires `term`.
  - MVP: static localized hint or rotate from Top Tags. Later: add trending terms endpoint.
- Promotions row (“Ưu đãi”)
  - Desired: featured brand/program deals.
  - API today: no `/promotions` or `/home` aggregation. See backend-proposals deferral.
  - MVP: derive lightweight “promotions” from `GET /api/v1/restaurants/search` and randomly pick a few; show as simple cards without discount badges.
- Suggestions for you
  - Desired: history-, behavior-, or trend-based chips.
  - API today: `GET /api/v1/tags/top` provides trending tags; no personalization.
  - MVP: combine local recent searches (client storage) + Top Tags.
- Banner
  - API today: none.
  - MVP: static asset or remote-config placeholder; deep links can be wired later.
- Recently ordered
  - API today: `GET /api/v1/orders/my` (auth required) with pagination.
  - MVP: show most recent restaurants from last N orders (dedupe); tap → details.
- Food categories shortcuts
  - API today: no categories endpoint (frontend-owned per backend-proposals). Use cuisine/tag mapping.
  - MVP: static curated categories → route to category list using `/restaurants/search?cuisine=...` or tag filters.

### 2) Typing state (keyboard open, realtime suggestions)
- Autocomplete list under search bar
  - API today: `GET /api/v1/search/autocomplete?term=&types=&limit=`.
  - MVP: debounce 250–300ms, show mixed types by default; optionally filter types per UI tab/context.

### 3) Results after submit (example: term = “cơm tấm”)
- Filters and quick chips
  - Supported now: `openNow` (universal search), `minRating` (restaurants), `priceBands`, `tags`, `cuisines`, `sort` (relevance|distance|rating|popularity), geo (`lat`/`lon`).
  - Not supported: “Khuyến mại/discount-only”, loyalty “bePoint”.
  - MVP: expose supported filters; hide discount/loyalty chips or show disabled placeholders.
- Results list content needs
  - Restaurant basics: name, rating, review count, distance → available via `/restaurants/search` or badges in `/search`.
  - Delivery ETA and fee → not in discovery endpoints.
  - Item price/discount on restaurant tile → not returned by `/restaurants/search`.
  - Quick add “+” to cart → requires menu context/customizations; not feasible from search without selecting item.
  - MVP: show restaurant-focused list (name, rating, ratingCount, distanceKm, optional logo). Tap → restaurant details to order. Skip ETA/fee/discount and quick add.
- Tabs “All | Restaurants | Menu Items” (optional)
  - API today: `/search` with `entityTypes` can power All and Menu Items; `/restaurants/search` powers Restaurants.
  - MVP: start with a single Restaurants tab (simpler) or two tabs (Restaurants + Menu Items). All-tab can wait.

## Confirmed API Coverage (usable now)
- Universal search: `GET /api/v1/search` (entityTypes, facets via `includeFacets`, geo, sorts).
- Autocomplete: `GET /api/v1/search/autocomplete` (limit, types).
- Restaurants search: `GET /api/v1/restaurants/search` (q, cuisine, tags/tagIds, minRating, geo, sorts including popularity/distance).
- Tags: `GET /api/v1/tags/top` (seed chips for suggestions).
- Restaurant info/menu/reviews: `GET /api/v1/restaurants/{id}/{info|menu|reviews|reviews/summary}`.
- Order history (for “recently ordered”): `GET /api/v1/orders/my`.

## Gaps That Affect This UX
- Promotions/banners
  - No `/promotions` or `/home` endpoint; no discount metadata in search results.
  - Impact: cannot reliably show true discounts; banner content must be static.
- Discount/loyalty filters
  - No query flags for “discounted only” or “bePoint loyalty”.
  - Impact: quick chips must be hidden or non-functional in MVP.
- Delivery ETA/fee in search results
  - Not present in discovery endpoints.
  - Impact: omit ETA/fee labels on results; keep UI clean and consistent.
- Quick add from results
  - No entity in results to add directly (needs specific menu item and customization flow).
  - Impact: route users to Restaurant Details to add items.
- Server-side trending searches/personalization
  - No endpoint for trending terms or personalized suggestions.
  - Impact: use Top Tags + local history only.
- Facets for `/restaurants/search`
  - Universal search has facets; restaurants search does not expose facet counts.
  - Impact: filter UI shows toggles without counts; acceptable for MVP.

## MVP Scope (build now)
- Search entry page
  - Static/rotating placeholder; sections: Suggestions (Top Tags + local recents), Recently Ordered (if authed), optional simple “Promotions” row from randomized restaurants search, static banner.
- Autocomplete while typing
  - Wire `GET /search/autocomplete`, debounce, keyboard-aware list.
- Results page (initial cut)
  - Query → Restaurants list via `/restaurants/search` with pagination and sorts (relevance default; distance when geo present; rating as option).
  - Filters: minRating, openNow (if we use universal search for an “All” tab), priceBands, tags. Start with minRating + tags + distance sort.
  - Card data: name, rating, ratingCount, distanceKm, optional logo. Tap → details.
- Optional: Menu Items tab using `/search?entityTypes=menu_item` with basic row (name, restaurantName, price if present; otherwise omit price).

## Phase Later (nice-to-haves / post-MVP)
- All-tab combining entities with badges and type pills.
- Promotions/collections pages (requires backend or curation tools).
- Discount-only and loyalty filters with real data.
- ETA/fee labels in lists (needs a lightweight quote/estimate service).
- Recent trends endpoint and basic personalization.
- Faceted filters for restaurants search (counts per cuisine/tag/priceBand).
- Aggregated `GET /restaurants/{id}/details` to speed up details page load.

## Frontend Workarounds and Placeholders
- Promotions row: pick N random restaurants from `/restaurants/search?pageSize=50` and display as plain brand tiles (no discount labels).
- Banner: show static image + optional deep link to a generic list (e.g., `/list/restaurants?source=promo`) backed by `/restaurants/search` with a preset filter.
- Recent searches: store last 10 terms in Hive (per user/device) and render as chips.
- Discount/loyalty chips: hidden for MVP; keep code paths behind feature flags.
- ETA/fee: suppressed; show distance only when geo is available.

## Backend Priorities (would materially improve UX)

Purpose: These changes directly uplift the three search screens (entry, typing, results) with high UX and conversion impact for minimal scope. Ordered by ROI and feasibility for an MVP+ iteration.

Overall notes:
- P1 Promotions API (lightweight)
  - `GET /api/v1/promotions` returning small cards: title, image, deeplink, optional restaurantId/collectionId; support “See all”.
  -> Use the new '## Home: Active Deals' endpoint implemented by the backend team. Read the API documentation for details.
- P2 Discount signals in search
  - Add `hasDiscount=true` filter and per-restaurant discount badge metadata (e.g., min %-off, promo tagline).
  -> See the updated `### Search Restaurants` section in the API documentation for details. The `discountedOnly` parameter has been added to the `GET /api/v1/restaurants/search` endpoint.
- P3 Restaurants facets
  - Facet counts for cuisines/tags/priceBands in `/restaurants/search` to power filter sheets.
  -> See the updated `### Search Restaurants` section in the API documentation for details. The `includeFacets` parameter has been added to the `GET /api/v1/restaurants/search` endpoint and will return informative facets.
- P4 Trends endpoint
  - `GET /api/v1/search/trending` returning terms/tags/brands by region, with TTL.
  -> No changes from the API. Frontend can resolve this follow the MVP plan.
- P5 ETA/fee preview
  - Lightweight quote endpoint for list contexts (lat/lon, restaurantId → ETA range + estimated fee).
  -> No changes from the API. Frontend can resolve this follow the MVP plan.
- P6 Aggregated details endpoint
  - `GET /api/v1/restaurants/{id}/details` bundling info+menu+reviewSummary with ETag support.
  -> No changes from the API. Frontend use the existing endpoints as per the MVP plan.

### Priority and effort snapshot
- High impact, low/medium effort: P1 Promotions, P3 Facets, P4 Trends.
- Medium impact, medium effort: P6 Aggregated Details.
- High impact, higher effort/operational maturity: P5 ETA/Fee Preview.

### Acceptance signals (post-release)
- P1: Card CTR and search-entry-to-details conversion lift.
- P2: Filter usage rate; discount badge CTR; average order value change.
- P3: Filter application frequency; reduction in empty-result bounces.
- P4: Suggestion chip tap rate; reduced keystrokes to search.
- P5: Conversion from results to cart; cancellation rate drop after ETA disclosure.
- P6: Details page TTI reduction and scroll-ready time improvement.

## Technical Notes (client)
- State & DI
  - New feature module `features/search` with `SearchViewModel` and `SearchResultsViewModel` using Provider.
  - Repository: `SearchRepository` with methods for autocomplete, universal search, restaurants search, tags top.
- Networking
  - Use existing `ApiClient` and interceptors. Add endpoints constants in `home_api_endpoints.dart` or a new `search_api_endpoints.dart`.
  - Debounce autocomplete; cache last results for a short TTL.
- Navigation
  - Routes: `/search` (entry) and `/search/results?q=`.
  - Preserve query in the search bar and in view model state when navigating back.
- Pagination
  - Use pageNumber/pageSize; implement “Load more” or infinite scroll with loading guards.
- Files to leverage
  - Models already present: `lib/features/home/data/models/search_page_response.dart`, `lib/features/home/data/models/restaurants_search_response.dart`, `lib/features/home/data/models/menu_items_feed_response.dart`.

## Acceptance Criteria (MVP)
- Typing shows suggestions within 300ms and selecting one opens results with persisted query.
- Restaurants results support pagination and at least one sort (rating or distance when geo present).
- Filters sheet provides at least minRating and tags; no discount/loyalty in MVP.
- Entry page shows Suggestions (Top Tags + local history) and Recently Ordered when authenticated.
- No crashes or layout overflows when keyboard opens/closes; empty/error states handled.

## Risks & Mitigations
- Perceived “promo” accuracy without true discounts
  - Mitigation: label the row as “Featured Picks” and avoid discount wording.
- Mixed search types complexity
  - Mitigation: start with Restaurants tab only; expand later.
- Latency on low-end devices
  - Mitigation: skeletons, image caching, list virtualization, and request debouncing.

