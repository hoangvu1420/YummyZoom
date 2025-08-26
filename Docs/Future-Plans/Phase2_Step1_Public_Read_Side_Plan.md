## Phase 2 – Step 1: Public Read Side First (Plan & Design)

### Goals (outcomes)
- Deliver fast, cache-friendly public APIs backed by the `FullMenuView` read model:
  - `GET /api/public/restaurants/{restaurantId}/menu` with ETag/Last-Modified
  - `GET /api/public/restaurants/{restaurantId}/info`
  - `GET /api/public/restaurants/search` (stubbed; pagination + filters contracted)
- Provide a temporary admin-only rebuild command for a restaurant’s `FullMenuView` to validate end-to-end early.

### Non-goals (for later steps)
- Management/write-side commands and UI flows
- Projector/event coverage beyond what’s needed to rebuild a single restaurant on demand

---

### Read Model: `FullMenuViews`
- Source of truth for public menu payloads; returns pre-baked JSON.
- Columns (reference): `RestaurantId (PK)`, `MenuJson (nvarchar(max)/json)`, `LastRebuiltAt (datetimeoffset)`, optional `MenuJsonHash`.
- Indexing: `PK(RestaurantId)`; optional nonclustered index on `LastRebuiltAt` for monitoring and housekeeping.
- SLA: Rebuild on changes (eventually consistent). Public side treats it as immutable between rebuilds.

### Data Contract (response shape)
- `MenuJson` uses the refined, normalized structure (v1) below to minimize payload size, enable stable ordering, and support future partial patches.
- Response envelope for menu endpoint:
  - Body: raw `MenuJson` string (already JSON), or `{ menuJson, lastRebuiltAt }` if envelope preferred.
  - Headers: `ETag`, `Last-Modified`, `Cache-Control`.

Refined MenuJson structure (v1)
```json
{
  "version": 1,
  "restaurantId": "<uuid>",
  "menuId": "<uuid>",
  "menuName": "<string>",
  "menuDescription": "<string>",
  "menuEnabled": true,
  "lastRebuiltAt": "<ISO8601>",
  "currency": "<3-letter ISO>",

  "categories": {
    "order": ["<categoryId>", "<categoryId>"],
    "byId": {
      "<categoryId>": {
        "id": "<uuid>",
        "name": "<string>",
        "displayOrder": 1,
        "itemOrder": ["<itemId>", "<itemId>"]
      }
    }
  },

  "items": {
    "byId": {
      "<itemId>": {
        "id": "<uuid>",
        "categoryId": "<uuid>",
        "name": "<string>",
        "description": "<string>",
        "price": { "amount": 12.34, "currency": "USD" },
        "imageUrl": "<string|null>",
        "isAvailable": true,
        "dietaryTagIds": ["<uuid>", "<uuid>"]
        ,"customizationGroups": [
          { "groupId": "<uuid>", "displayTitle": "<string>", "displayOrder": 1 }
        ]
      }
    }
  },

  "customizationGroups": {
    "byId": {
      "<groupId>": {
        "id": "<uuid>",
        "name": "<string>",
        "min": 0,
        "max": 2,
        "options": [
          {
            "id": "<uuid>",
            "name": "<string>",
            "priceDelta": { "amount": 0.50, "currency": "USD" },
            "isDefault": false,
            "displayOrder": 1
          }
        ]
      }
    }
  },

  "tagLegend": {
    "byId": {
      "<tagId>": { "name": "Vegan", "category": "Dietary" }
    }
  }
}
```

Notes:
- Normalized maps (`byId`) with explicit `order` arrays avoid duplication and enable efficient diff/patch.
- `dietaryTagIds` references are resolved via `tagLegend` to avoid repeating tag names across many items.
- `currency` is the restaurant/menu default; prices also carry currency to allow future multi-currency scenarios.
- Exclude soft-deleted entities and disabled menus; include only currently visible categories/items.

---

### Application Layer – Queries (Dapper)
- `GetFullMenuQuery` (Inputs: `RestaurantId`) → Output: `MenuJson`, `LastRebuiltAt`.
  - SQL: `SELECT MenuJson, LastRebuiltAt FROM FullMenuViews WHERE RestaurantId = @RestaurantId`.
  - If not found → 404.
- `GetRestaurantPublicInfoQuery` (Inputs: `RestaurantId`) → Output: minimal card (e.g., `Id`, `Name`, `LogoUrl`, `CuisineTags`, `IsOpen`, `City/Area`).
  - Source: existing Restaurant/Brand tables (or temporary stub provider if data model incomplete).
  - If not found or disabled → 404.
- `SearchRestaurantsQuery` (Inputs: `q?`, `cuisine?`, `lat?`, `lng?`, `radiusKm?`, `page`, `pageSize`) → Output: paginated list of minimal cards.
  - Initial stub: text/cuisine filters only; geo filter stubs/validations included; consistent pagination contract (cursor or offset + total count optional).
  - Reuse existing Dapper pagination helpers.

Implementation notes
- Keep handlers thin; validate inputs with FluentValidation (e.g., GUIDs, ranges, pageSize caps).
- Map Dapper rows to readonly DTOs; do not parse `MenuJson` in the query handler.

---

### DTOs and Contracts (Application layer)
- Follow the Orders query DTO approach in `src/Application/Orders/Queries/Common/OrderDtos.cs` (readonly record DTOs, flattened monetary fields for performance).
- DTO definitions (new):
  - `src/Application/Public/Queries/Common/MenuDtos.cs`
    - `FullMenuViewRow` (internal): `string MenuJson`, `DateTimeOffset LastRebuiltAt` (for Dapper materialization only)
    - `RestaurantPublicInfoDto`: `Guid RestaurantId`, `string Name`, `string? LogoUrl`, `IReadOnlyList<string> CuisineTags`, `bool IsOpen`, `string? City`, `string? Area`
    - `RestaurantSearchResultDto`: `Guid RestaurantId`, `string Name`, `string? LogoUrl`, `IReadOnlyList<string> CuisineTags`, `decimal? AvgRating`, `int? RatingCount`, `string? City`, `string? Area`
- Validators:
  - `GetFullMenuQueryValidator`: validates `RestaurantId` is GUID
  - `GetRestaurantPublicInfoQueryValidator`: validates `RestaurantId`
  - `SearchRestaurantsQueryValidator`: validates `page/pageSize` ranges, optional filters length, geo parameter coherence

Notes
- The menu endpoint returns raw `MenuJson` (no DTO body) for speed; the DTO is used only for DB row mapping.
- Restaurant info and search return typed DTOs optimized for public cards.

---

### Web Endpoints (Public)
- `GET /api/public/restaurants/{restaurantId}/menu`
  - Reads via `GetFullMenuQuery`.
  - Caching:
    - `ETag`: weak ETag derived from `LastRebuiltAt` (e.g., `W/"r:{restaurantId}:t:{ticks}"`).
    - `Last-Modified`: from `LastRebuiltAt` (RFC1123).
    - `Cache-Control`: `public, max-age=300` (tunable).
    - Conditional requests: respect `If-None-Match`/`If-Modified-Since` → 304.
  - Response body: stream/write `MenuJson` directly with `application/json` to avoid double-encoding.
  - Errors: 404 if view missing; 412 if invalid conditional headers; 400 for invalid `restaurantId`.

- `GET /api/public/restaurants/{restaurantId}/info`
  - Returns minimal restaurant card; 404 for missing/disabled.

- `GET /api/public/restaurants/search`
  - Accepts filters; returns paginated minimal cards; stub implementation is acceptable initially but must keep final contract.

Placement & conventions
- Endpoints file organization follows existing pattern in `src/Web/Endpoints/`. Add public routes in `Restaurants.cs` under a Public region, or optionally add `Restaurants.Public.cs` for separation if partials are enabled.
- Query handlers under `src/Application/Public/Queries/...` following CQRS folder convention.

---

### File manifest (to create/update)
- Application (Queries)
  - `src/Application/Public/Queries/Common/MenuDtos.cs`
  - `src/Application/Public/Queries/GetFullMenu/GetFullMenuQuery.cs`
  - `src/Application/Public/Queries/GetFullMenu/GetFullMenuQueryHandler.cs`
  - `src/Application/Public/Queries/GetFullMenu/GetFullMenuQueryValidator.cs`
  - `src/Application/Public/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQuery.cs`
  - `src/Application/Public/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQueryHandler.cs`
  - `src/Application/Public/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQueryValidator.cs`
  - `src/Application/Public/Queries/SearchRestaurants/SearchRestaurantsQuery.cs`
  - `src/Application/Public/Queries/SearchRestaurants/SearchRestaurantsQueryHandler.cs`
  - `src/Application/Public/Queries/SearchRestaurants/SearchRestaurantsQueryValidator.cs`
- Web
  - Update `src/Web/Endpoints/Restaurants.cs` to add:
    - `GET /api/public/restaurants/{restaurantId}/menu`
    - `GET /api/public/restaurants/{restaurantId}/info`
    - `GET /api/public/restaurants/search`
  - `src/Web/Infrastructure/Http/HttpCaching.cs` (new): ETag/Last-Modified helpers
- Application (Admin command)
  - `src/Application/Admin/Commands/RebuildFullMenu/RebuildFullMenuCommand.cs`
  - `src/Application/Admin/Commands/RebuildFullMenu/RebuildFullMenuCommandHandler.cs`
  - `src/Application/Admin/Commands/RebuildFullMenu/RebuildFullMenuCommandValidator.cs`
- Infrastructure (Assembler for manual rebuild)
  - `src/Infrastructure/ReadModels/FullMenu/FullMenuAssembler.cs` (loads authoritative tables, composes refined `MenuJson`)

Optional (if desired later)
- `src/Web/Endpoints/Restaurants.Public.cs` (partial) for public routes separation.
- `src/Infrastructure/ReadModels/FullMenu/Sql/` for extracted SQL files (if not inlined in handlers).

---

### Temporary Admin Command (manual rebuild)
- Purpose: enable early end-to-end validation before full projector coverage.
- Endpoint: `POST /api/admin/restaurants/{restaurantId}/fullmenu/rebuild`
  - Auth: `[Authorize(Policy = Policies.MustBePlatformAdmin)]`.
  - Behavior: load authoritative data, compose menu DTO, serialize JSON, upsert `FullMenuViews`, set `LastRebuiltAt = UtcNow`.
  - Idempotent: repeated calls overwrite same row; safe under concurrency.

Implementation details
- Use `FullMenuAssembler` to fetch: enabled `Menu` for restaurant, visible `MenuCategories` ordered, `MenuItems` filtered by `IsAvailable` and not deleted, `CustomizationGroups` with `Choices`, and `Tags` referenced by items.
- Assemble according to refined `MenuJson (v1)` using normalized maps, ensuring stable `order` arrays.
- Persist via EF Core upsert into `FullMenuViews` with `jsonb` `MenuJson` and `LastRebuiltAt` set to `UtcNow`.

---

### Caching & Performance Strategy
- Targets: p95 < 100 ms, median < 30 ms; payload < 1 MB (prefer < 500 KB); 304 hit rate > 60% on hot menus.
- Compression: enable gzip/br; set `Vary: Accept-Encoding`.
- ETag design: weak ETag based on `LastRebuiltAt` ticks; switchable to content hash if needed.
- Database: single point read by `RestaurantId` with clustered PK; no joins for menu endpoint.
- HTTP: 304 short-circuit to avoid body and DB when `If-None-Match` matches cached ETag.

---

### Security, Validation, and Limits
- Public endpoints: anonymous allowed; validate GUIDs and numeric ranges.
- Rate limiting: per-IP and per-restaurant bucket (protect hot endpoints).
- Tenant safety: only reads public data; ensure restaurant is not soft-deleted/disabled (check flag in info query).
- Input caps: `pageSize` ≤ 50; string `q` length ≤ 100; `radiusKm` ≤ 25.

---

### Observability
- Metrics: request count, latency, DB time, payload bytes, 304 ratio, cache hit/miss.
- Logs: structured with `restaurantId`, `etag`, response status (304/200/404), and `dbElapsedMs`.
- Tracing: spans for query execution and JSON write.

---

### Testing Plan
- Unit: ETag builder, validators, pagination helpers.
- Functional: endpoint 200/304/404 paths, header semantics, content-type, rate limit behavior (where feasible).
- Integration: Dapper queries against test DB with seeded `FullMenuViews`.
- E2E: admin rebuild → public GET returns JSON and correct headers.
- Load: quick smoke (e.g., 100 RPS for 60s) to validate latency and 304 effectiveness.

---

### Rollout Plan
- Behind feature flag for public endpoints.
- Start with `max-age=60`, observe metrics, then raise to 300 if stable.
- Enable 304 logic immediately; monitor 304 ratio and error rates.
- Backfill job (step 8) later; for step 1, rely on manual rebuilds for selected restaurants.

---

### Risks & Mitigations
- Large `MenuJson` increases latency → split images/large blobs into CDN URLs only; trim fields.
- Clock skew affects `If-Modified-Since` → prefer ETag; accept small inconsistencies.
- Event lag leaves stale views → manual rebuild available; projector in step 2.
- Hotspot traffic on few restaurants → CDN caching via public cache headers; rate limit.

---

### Deliverables (Step 1)
- Queries: `GetFullMenuQuery`, `GetRestaurantPublicInfoQuery`, `SearchRestaurantsQuery` (stub).
- DTOs: menu row mapping, restaurant info/search DTOs; validators for each query.
- Web endpoints: three public GETs with caching headers.
- Admin-only manual rebuild command/endpoint.
- Validators, ETag helper, and tests (unit/functional/integration/E2E).

---

### Work Breakdown (implementation order)
1) Create DTOs and validators; implement query handlers (Dapper + inline SQL).
2) ETag/Last-Modified helper and header utilities.
3) `GET /api/public/restaurants/{restaurantId}/menu` endpoint (stream raw `MenuJson`).
4) `GET /api/public/restaurants/{restaurantId}/info` endpoint.
5) `GET /api/public/restaurants/search` endpoint (stub with final contract).
6) Admin rebuild command + endpoint; implement `FullMenuAssembler`.
7) Tests and basic load check; tune `Cache-Control`.
