# Phase 2 Discovery & Menu – MVP Gap Analysis

Project: YummyZoom Customer App
Date: 2025-10-10
Sources: Docs/Architecture/YummyZoom_Project_Documentation.md, Docs/Frontend-Team-Responses/phase2_discovery_api_analysis.md

## Executive Summary
- We can deliver an MVP for Phase 2 using current public endpoints with small client-side fallbacks and without introducing new aggregation endpoints.
- Defer most aggregation and content curation features (home dashboard, categories taxonomy, promotions, collections) to a fast-follow. Use multi-call fallbacks and/or static client lists for launch markets.
- No blocking backend work is strictly required for MVP if we scope out map-viewport filtering and advanced reviews filters. Optional low-effort enrichments are noted below to improve UX/perf.

## What Ships Now (Using Current APIs)
- Home (basic): populate using multiple calls
  - Featured/Nearby: `GET /api/v1/restaurants/search` with `lat/lon` (when available) and simple sorts.
  - Categories: temporary static list in client per launch market; link to filtered search views.
  - Promotions/Collections: omit for MVP (placeholder UI allowed).
- Search: 
  - Autocomplete: `GET /api/v1/search/autocomplete` with client debounce + cache.
  - Results: `GET /api/v1/search` (no bbox; basic sort). Tabs can be UI-only filters against existing results if entity typing is limited.
- Categories Directory/List:
  - Directory: use the static categories list (client) for links.
  - List: `GET /api/v1/restaurants/search` with mapped cuisine/tag filters; paginate client-side using server pagination.
- Nearby:
  - List-first experience using `lat/lon` on `GET /api/v1/restaurants/search`. Defer map viewport (bbox) to fast-follow.
- Restaurant Details:
  - Info: `GET /api/v1/restaurants/{id}/info`.
  - Menu: `GET /api/v1/restaurants/{id}/menu` (respect ETag/Last-Modified).
  - Review summary: `GET /api/v1/restaurants/{id}/reviews/summary`.
  - Full Reviews page: `GET /api/v1/restaurants/{id}/reviews` without advanced filters.

## Gap Decisions (By Frontend Proposals)
- P1 Home Dashboard Aggregation (`/api/v1/home`) – Defer
  - MVP: multi-call fallback; categories static; skip promos. Revisit aggregation for perf after MVP.
- P2 Category Taxonomy (`/api/v1/categories`) – Defer
  - MVP: ship with static categories list in client; map to search filters.
- P3 Restaurants by Category (`/categories/{id}/restaurants`) – Defer
  - MVP: use `restaurants/search` with tag/cuisine mapping and pagination.
- P4 Promotions/Banners (`/promotions`) – Defer
  - MVP: omit; non-blocking for discovery and menu browsing.
- P5 Restaurant Details Aggregation (`/restaurants/{id}/details`) – Defer
  - MVP: 2–3 granular calls in parallel; caching already supported on menu.
- P6 Enrich Restaurant Info (rating, priceBand, coords, hours) – Optional Fast-Follow
  - MVP: derive rating from `reviews/summary`; omit price band/hours if unavailable; coords not required if map is deferred.
- P8 Search Enhancements (entityTypes, sort breadth, bbox, fields projections) – Partial Defer
  - MVP: keep current search; skip bbox and advanced sorts. If trivial, allow `limit` for autocomplete.
- P9 Reviews Filters/Sort – Defer
  - MVP: basic list; filters/sorting not required for first release.

## Minimal Backend Enhancements (Nice-to-Have, Not Blocking)
- Add `limit` to `/search/autocomplete` with a safe default (e.g., 10).
- Ensure `restaurants/search` supports simple `sort=rating|distance` gracefully; if distance not computed, return server default order.
- Document optional fields for `/restaurants/{id}/info` without breaking existing clients (avgRating, ratingCount) to remove one extra call later.

## UX/Delivery Trade-offs for MVP
- Categories: static curated list per market means manual updates until taxonomy API lands.
- Home: multiple calls may increase TTI; mitigate with request batching and skeleton UI.
- Nearby Map: list-first experience; add map + bbox server support as fast-follow.
- Details: one extra request for ratings; acceptable given parallelization and caching.

## Fast-Follow Backlog (Post-MVP Priorities)
1) `/home` aggregation with promotions and featured logic.
2) Category taxonomy + restaurants-by-category endpoints.
3) Search enhancements: entity typing, bbox, projections, richer sorts.
4) Details aggregation and Info enrichment (price band, coords, hours) for fewer round-trips.
5) Reviews filters/sorting for the Reviews page.

## Risks & Mitigations
- Content Curation Gap (no promotions/collections):
  - Mitigation: hide or show placeholder; ship when `/promotions` is ready.
- Static Categories Drift:
  - Mitigation: config-driven list with build-time validation; schedule regular review.
- Performance (Home without aggregation):
  - Mitigation: parallel fetch + cache; move to `/home` in fast-follow.

## Open Questions to Resolve (Non-Blocking for MVP)
- Categories ownership and localization workflow once taxonomy ships.
- Featured restaurants definition (editorial flag/score) for `/home`.
- Geo defaults when `lat/lon` absent (market fallback rules).
- Error shape adoption (RFC 7807) timing vs current clients.

---
Decision: Proceed with MVP using existing endpoints and the fallbacks above. Create tickets for fast-follow items 1–5 and schedule after MVP code-freeze.
