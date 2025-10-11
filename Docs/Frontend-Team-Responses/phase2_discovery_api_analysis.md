## Phase 2 — Backend API Analysis & Synchronization (Discovery & Menu)

Project: YummyZoom Customer App
Date: 2025-10-10
Related:
- docs/backend-proposals/phase1_auth_api_analysis.md (Phase 1 analysis)
- docs/future/phase2_discovery_flow_proposals.md (Discovery flow)

---

### 1) Scope & Goal (Step 0 of Standard Workflow)

Goal: Validate that existing public APIs fully support Phase 2 features and identify any changes needed before client-side implementation.

Phase 2 features from roadmap + enhanced discovery flow:
- Home: categories, promotions, featured/nearby + “See all” links
- Search: entry, autocomplete, results (tabs All/Restaurants/Menu Items) with filters/sort and list/map toggle
- Categories Directory: all categories grid → category lists
- Category Restaurant List: filters/sort, pagination, optional map toggle
- Nearby: map + draggable list with category chips, filters/sort
- Restaurant Details: info + menu (+ review summary) and Reviews page

API docs reviewed (docs/API-Documentation):
- API-Reference/Customer/02-Restaurant-Discovery.md
- 00-Introduction.md, 01-Getting-Started.md, 03-Core-Concepts.md
- API-Reference/Customer/01-Authentication-and-Profile.md (shared conventions)

---

### 2) Current Coverage (What we can build now)

Existing endpoints cover most of Restaurant Discovery and Menu browsing:
- GET /api/v1/search — universal search across restaurants, items, tags
- GET /api/v1/search/autocomplete — typeahead suggestions
- GET /api/v1/restaurants/search — restaurant list with filters (q, cuisine, lat/lng, minRating, pagination)
- GET /api/v1/restaurants/{restaurantId}/info — basic restaurant info (name, logoUrl, cuisineTags, isAcceptingOrders, city)
- GET /api/v1/restaurants/{restaurantId}/menu — normalized menu (categories, items, customization groups)
- GET /api/v1/restaurants/{restaurantId}/reviews and /reviews/summary — ratings and reviews

Implication:
- Details can be built via Info + Menu (+ Review Summary). Menu supports HTTP caching.
- Category list can be approximated by /restaurants/search filters; a first-class category taxonomy is still missing.
- Home needs a single aggregated call; Search results benefit from explicit sort/projection; map mode benefits from bbox queries.
- Reviews page benefits from server-side star/withPhotos filters and sorting.

---

### 3) Simulated User Flows → Required Calls

Home
- Need: categories, promotions/banners, featured/nearby, recent/trending searches, collections
- Today: multiple calls; missing categories/promotions/collections contracts

Category List
- Need: list by categoryId with filters (openNow, minRating, priceBand), sort (rating, popularity, distance), pagination; optional map mode
- Today: generic search only

Restaurant Details
- Need: info, menu, review summary (and full reviews list on separate page)
- Today: 2–3 calls; Info lacks rating/hours/priceBand/coords

Search
- Need: autocomplete, universal search with entity type filters, sort, bbox/list projections
- Today: basic search + autocomplete; missing type filters, sort breadth, bbox, projections

Nearby
- Need: restaurants by lat/lon with distance, filters/sort; supports list/map
- Today: supported via /restaurants/search but distance and sort parameters should be explicit

---

### 4) Identified Gaps & Backend Change Proposals (Prioritized)

P1. Home Dashboard Aggregation
- Add GET /api/v1/home (Public)
- Returns: categories[], promotions[], featured.page; optionally nearby.page when lat/lon provided
- Add: recentSearches[], trendingSearches[], collections[] and per-module seeAllDeeplink
- Rationale: minimize round-trips and provide consistent content strategy

P2. Customer Category Taxonomy
- Add GET /api/v1/categories (Public)
- Model: { categoryId, slug, name, localizedNames?, iconUrl?, displayOrder, tagMappings? }
- Rationale: stable curated taxonomy for carousels and directory;

P3. Restaurants by Category
- Add GET /api/v1/categories/{categoryId}/restaurants (Public)
- Query: pageNumber, pageSize, openNow?, minRating?, priceBand?, sort?=popularity|rating|distance, lat, lon, bbox?
- Fields: include distanceKm when geo present; optional priceBand
- Rationale: explicit category contract; enables filters/sort and map mode

P4. Promotions/Banners for Home
- Add GET /api/v1/promotions (Public)
- Model: { id, title, subtitle?, imageUrl, imageAspect?, deeplink, placement?=home|restaurant_details, displayOrder, validFromUtc, validToUtc, targeting? { city?, lat?, lon?, radiusKm? } }
- Rationale: power promo banner surfaces with targeting and deep-links

P5. Restaurant Details Aggregation (Optional)
- Add GET /api/v1/restaurants/{restaurantId}/details (Public)
- Includes: info + menu + reviewSummary; caching via ETag/Last-Modified composition
- Rationale: reduce round-trips for first paint

P6. Enrich Restaurant Info
- Modify GET /api/v1/restaurants/{restaurantId}/info to optionally include:
  - avgRating, ratingCount
  - openingHours or isOpenUntil, nextOpenAt
  - priceBand (1–4), deliveryEtaMinutes?, deliveryFeeMoney?, minOrderMoney?
  - coordinates { lat, lon }
- Rationale: list cards and details header need these without extra calls

P7. Collections (Curated Lists)
- Add GET /api/v1/collections (Public)
- Add GET /api/v1/collections/{collectionId}/restaurants (Public)
- Model: { id, name, iconUrl?, description?, deeplink, sortHint?, filters? }
- Rationale: “See all” and curated discovery (Free Delivery, Top Rated Nearby, New on YummyZoom)

P8. Search Enhancements
- Extend GET /api/v1/search with:
  - entityTypes?=Restaurant|MenuItem|Tag (array)
  - sort?=relevance|rating|distance|priceBand|popularity
  - bbox?=minLon,minLat,maxLon,maxLat for map viewport
  - fields?=light|default|full projection
- Extend GET /api/v1/restaurants/search with:
  - priceBand?, openNow?, bbox?, sort?=rating|distance|popularity; return distanceKm when geo present
- Autocomplete: add limit? (default 10), types? filter; enable short TTL caching

P9. Reviews API (Read)
- Ensure GET /api/v1/restaurants/{id}/reviews supports:
  - Filters: stars?=1..5, withPhotos?=true|false
  - Sort: sort?=newest|highest|lowest|helpful
  - Pagination: pageNumber, pageSize
- Rationale: full Reviews page with filters/sorting aligned with summary

---

### 5) Open Questions for Backend

Q1. Categories vs Cuisines: curated taxonomy ownership and translations (who manages, cadence)?

Q2. Featured Restaurants: editorial flag/score exposure; filter `featured=true` and sort semantics?

Q3. Promotions Source: CMS vs DB; targeting rules format (geo/segment) and conflict resolution?

Q4. Details Aggregation: single-call vs granular calls — payload budget and caching strategy?

Q5. Location Signals: default market behavior when lat/lon absent; geo fallback rules?

Q6. Rating Consistency: authoritative source + update cadence for avgRating/ratingCount vs /reviews/summary?

Q7. Backward Compatibility: Info enrichment added as optional fields without breaking current clients?

Q8. Search Sorting & Projection: supported sort keys and `fields` projections for `/search` and `/restaurants/search`?

Q9. Map View Support: `bbox` server-side filtering vs client-side; any rate-limit implications?

Q10. Collections Ownership: curation workflow (CMS/static), localization, and desired release cadence?

Q11. Promotions Targeting: v1 requirement for geo targeting; header/flag to bypass targeting in QA?

---

### 6) Suggested Contracts (Draft)

GET /api/v1/home
```json
{
  "categories": [
    { "categoryId": "milk_tea", "slug": "milk-tea", "name": "Milk Tea", "localizedNames": {"vi-VN": "Trà sữa"}, "iconUrl": null, "displayOrder": 1 },
    { "categoryId": "coffee", "name": "Coffee", "iconUrl": null, "displayOrder": 2 }
  ],
  "promotions": [
    { "id": "p1", "title": "Free Delivery Week", "imageUrl": "https://cdn/.../p1.jpg", "deeplink": "/promo/free-delivery", "validFromUtc": "2025-10-10T00:00:00Z", "validToUtc": "2025-10-17T00:00:00Z", "displayOrder": 1 }
  ],
  "featured": {
    "items": [ { "restaurantId": "...", "name": "Mario's Italian Bistro", "logoUrl": "...", "avgRating": 4.5, "ratingCount": 127 } ],
    "pageNumber": 1, "totalPages": 3, "totalCount": 25, "hasPreviousPage": false, "hasNextPage": true
  },
  "recentSearches": ["pizza", "sushi"],
  "trendingSearches": ["burger", "fried chicken"],
  "collections": [ { "id": "c_free_delivery", "name": "Free Delivery", "deeplink": "/collections/c_free_delivery" } ]
}
```

GET /api/v1/categories
```json
[
  { "categoryId": "pizza", "slug": "pizza", "name": "Pizza", "localizedNames": {"vi-VN": "Pizza"}, "iconUrl": null, "displayOrder": 1 },
  { "categoryId": "sushi", "name": "Sushi", "iconUrl": null, "displayOrder": 2 }
]
```

GET /api/v1/categories/{categoryId}/restaurants
```json
{
  "items": [
    { "restaurantId": "...", "name": "Pasta Palace", "logoUrl": null, "avgRating": 4.2, "ratingCount": 89, "priceBand": 2, "distanceKm": 1.4, "city": "San Francisco" }
  ],
  "pageNumber": 1, "totalPages": 2, "totalCount": 15, "hasPreviousPage": false, "hasNextPage": true
}
```

GET /api/v1/restaurants/{restaurantId}/details
```json
{
  "info": { "restaurantId": "...", "name": "...", "logoUrl": "...", "cuisineTags": ["Italian"], "isAcceptingOrders": true, "city": "San Francisco", "avgRating": 4.5, "ratingCount": 127, "priceBand": 2, "coordinates": {"lat": 37.77, "lon": -122.41} },
  "menu": { "version": 1, "categories": { "order": ["..."], "byId": { } }, "items": { "byId": { } }, "customizationGroups": { "byId": { } } },
  "reviewSummary": { "averageRating": 4.3, "totalReviews": 127 }
}
```

GET /api/v1/collections
```json
[
  { "id": "c_free_delivery", "name": "Free Delivery", "description": "Top picks with zero delivery fee", "deeplink": "/collections/c_free_delivery" }
]
```

GET /api/v1/collections/{collectionId}/restaurants
```json
{
  "items": [ { "restaurantId": "...", "name": "...", "avgRating": 4.6, "ratingCount": 230, "priceBand": 2 } ],
  "pageNumber": 1, "totalPages": 3, "totalCount": 55, "hasPreviousPage": false, "hasNextPage": true
}
```

GET /api/v1/search (enhanced)
```http
GET /api/v1/search?term=pizza&entityTypes=Restaurant,MenuItem&sort=distance&lat=37.77&lon=-122.41&bbox=-122.52,37.70,-122.35,37.82&fields=light&pageNumber=1&pageSize=20
```

---

### 7) Client Integration Notes (Mobile)

- Prefer /home for initial content; fallback to categories + restaurants/search when absent
- Category list: use categories/{id}/restaurants; fallback: restaurants/search with mapped cuisine/tag
- Details: use /details when available; otherwise call info + menu + review summary in parallel
- Respect ETag/Last-Modified on menu; configure dio caching
- Cache autocomplete responses (short TTL) and debounce input
- Use `fields=light` for list views; `fields=full` for details (when supported)
- Prefer `bbox` for map mode; fallback to lat/lon + radius

---

### 8) Acceptance Criteria for Backend Readiness (Phase 2)

- Home content via single call (or documented multi-call fallback) including categories and promotions
- Category taxonomy and restaurants-by-category endpoints populated for launch markets
- Details retrievable in one round (preferred /details) or three calls with consistent payloads
- Info includes ratings/coords/priceBand OR clear guidance to use other endpoints
- All new endpoints documented with examples and RFC 7807 errors
- `/search` supports entityTypes, sort, bbox, fields; returns distanceKm when geo present
- `/restaurants/search` supports sort (rating|distance|popularity) and returns distanceKm with geo
- Reviews list supports filters (stars, withPhotos) and sorting with stable semantics
- Collections endpoints available or documented fallback via search filters

---

### 9) Risks & Mitigations

- Missing taxonomy/collections delay discovery → Ship with cuisines-as-categories and hide collections until ready
- Details payload bloat → Keep /details optional; leverage granular endpoints + caching
- Inconsistent ratings across Info/Summary → Treat Summary as source of truth; Info fields optional

---

Status: Ready for backend review. On approval, mobile team proceeds to Step 1 (UI) using this as the Phase 2 API contract baseline.

===

## Response from Backend Team

Scope: Discovery + Menu (MVP-first). Below are concise decisions and current status.

- P1 Home Aggregation (/home): Deferred for MVP. Use multi-call fallback. No backend work blocking.
- P2 Categories Taxonomy (/categories): Deferred. Client uses static list mapped to search for MVP.
- P3 Restaurants by Category: Deferred. Use `/restaurants/search` with tag/cuisine mapping.
- P4 Promotions/Banners: Deferred. Placeholder allowed; no API for MVP.
- P5 Restaurant Details Aggregation (/details): Deferred. Use info + menu + review summary parallel calls.
- P6 Enrich Restaurant Info: Partially done. `/restaurants/{id}/info` now returns optional `avgRating`, `ratingCount` (populated via review summary when available). No contract break.
- P8 Search Enhancements: Partially done.
  - `/restaurants/search` supports `sort=rating|distance`; returns `distanceKm` when geo present.
  - `bbox` and additional sort keys are out of scope for MVP.
  - Autocomplete: added `limit` (default 10; range 1–50).
- P9 Reviews Filters/Sort: Deferred. Reviews list remains basic for MVP.

Notes for Integration
- Nearby/List: prefer `/restaurants/search?lat=..&lng=..&sort=distance`; expect `distanceKm` in items.
- Search List Tabs: keep UI-only filters; server does not expose `entityTypes` yet.
- Details Header: `avgRating`/`ratingCount` present when summary exists; otherwise null.
- Autocomplete: send `limit` as needed; debounce + short TTL cache recommended.


---

## Backend Update – Home (Lite) Available (2025-10-10)

- Decision: Add `/api/v1/home` aggregator (Lite) for MVP to reduce round-trips.
- Scope now:
  - `featured`: top restaurants by rating (server-side), paginated; shape matches `restaurants/search` items.
  - `nearby`: present only when `lat`/`lng` provided; sorted by distance; items include `distanceKm`.
  - `categories`, `promotions`, `collections`, `recentSearches`, `trendingSearches`: placeholders (empty arrays) until corresponding backends land.
- Params: `lat`, `lng` (optional), `pageSize` (default 10, max 20).
- Caching: none (for now). Consider short TTL after MVP if needed.
- Integration: prefer `/home` at app launch; fall back to multi-call if unavailable.

---

## Backend Update – Search Enhancements Delivered (2025-10-10)

- Universal Search (`GET /api/v1/search`)
  - New params: `entityTypes` (Restaurant|MenuItem|Tag), `sort` (relevance|distance|rating|priceBand|popularity), `bbox` (minLon,minLat,maxLon,maxLat).
  - Notes: `distance` sort requires `lat`/`lon`. `bbox` filters viewport; distanceKm still needs `lat`/`lon`.
- Restaurants Search (`GET /api/v1/restaurants/search`)
  - New: `sort=popularity`; `bbox` viewport filter. Existing `distance` and `rating` sorts remain.
- Autocomplete (`GET /api/v1/search/autocomplete`)
  - New: `types` filter (Restaurant|MenuItem|Tag). `limit` retained (default 10; 1–50).

Integration tips
- Map view: use `bbox` + short page sizes (≤20). Keep `lat`/`lon` when needing distance sort/labels.
- List view: prefer `sort=rating` or `sort=popularity`.
- Autocomplete: filter `types` per tab context and keep client debounce + TTL cache.
