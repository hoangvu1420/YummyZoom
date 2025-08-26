### Test suites and patterns to apply
- Contract tests (tests/Web.ApiContractTests)
  - Pattern: Replace MediatR with `CapturingSender`, simulate auth if needed, no real DB; assert route/verb/status/body/headers per WebApi_Contract_Tests_Guidelines.
- Functional tests (tests/Application.FunctionalTests)
  - Pattern: In-memory host + Testcontainers DB, real handlers/DB; set up data via factories; use `Testing` facade per Application-Functional-Tests-Guidelines.

### Scope covered by implementation
- Public endpoints in `src/Web/Endpoints/Restaurants.cs`:
  - GET `/api/restaurants/{restaurantId}/menu` with ETag/Last-Modified/Cache-Control and 304 support.
  - GET `/api/restaurants/{restaurantId}/info`.
  - GET `/api/restaurants/search`.
- Handlers and validators (Dapper queries):
  - `GetFullMenuQueryHandler`, `GetRestaurantPublicInfoQueryHandler`, `SearchRestaurantsQueryHandler`.
  - Validators for the above queries.
- HTTP caching helper: `src/Web/Infrastructure/Http/HttpCaching.cs`.
- Admin command handler exists (`RebuildFullMenuCommandHandler`); only command, no endpoint for now.

### Contract tests (HTTP facade only)
Create under `tests/Web.ApiContractTests/Public/Restaurants/`:

- Get menu: GET `/api/restaurants/{id}/menu`
  - 200 OK returns raw JSON body
    - Arrange: `factory.Sender.RespondWith(_ => Result.Success(new GetFullMenuResponse("{\"version\":1}", rebuiltAt)))`.
    - Assert:
      - Status 200, Content-Type `application/json`
      - Body equals the raw JSON string (not double-encoded)
      - Headers set: `ETag`, `Last-Modified`, `Cache-Control` = `public, max-age=300`
      - ETag format per contract: weak ETag `W/"r:{restaurantId}:t:{ticks}"` and `Last-Modified` RFC1123.
      - Mapping: `factory.Sender.LastRequest` is `GetFullMenuQuery` with same `restaurantId`.
  - 304 Not Modified with If-None-Match
    - Arrange canned success as above; send request with `If-None-Match` equal to computed ETag.
    - Assert 304, no body; headers include `ETag`, `Last-Modified`, `Cache-Control`.
  - 304 Not Modified with If-Modified-Since
    - Arrange success; send request with `If-Modified-Since` >= `LastRebuiltAt`.
    - Assert 304 + headers.
  - 404 Not Found
    - Arrange: `Result.Failure<GetFullMenuResponse>(Error.NotFound("Public.GetFullMenu.NotFound", "Missing"))`.
    - Assert 404 ProblemDetails, `title` = "Public".
  - 400 Invalid route id
    - Call `/menu` with non-GUID id (binding should produce 400). Assert 400 ProblemDetails.
  - Consideration: The code’s ETag builder currently produces `W"r:...` (missing slash). The contract test should expect `W/"...` per the plan; that will surface the discrepancy.

- Get restaurant info: GET `/api/restaurants/{id}/info`
  - 200 OK with DTO shape
    - Arrange: canned `RestaurantPublicInfoDto`.
    - Assert status 200, JSON with primitive GUID for `restaurantId` and fields: `name`, `logoUrl`, `cuisineTags` (array), `isOpen`, `city`, `area`.
    - Assert LastRequest is `GetRestaurantPublicInfoQuery` with id.
  - 404 Not Found: return typed `Result.Failure<RestaurantPublicInfoDto>(Error.NotFound(...))`, assert 404 ProblemDetails.
  - 400 Invalid route id: non-GUID path segment → 400 ProblemDetails.

- Search restaurants: GET `/api/restaurants/search`
  - 200 OK, pagination and item shape
    - Arrange: canned `PaginatedList<RestaurantSearchResultDto>`.
    - Assert 200, array item fields (primitive GUIDs), page metadata.
    - Assert LastRequest equals `SearchRestaurantsQuery` with bound query params you pass (e.g., `q`, `cuisine`, `pageNumber`, `pageSize`).
  - 400 Validation failure envelope
    - Arrange a `Result.Failure<PaginatedList<RestaurantSearchResultDto>>(Error.Validation("Public.Search.Invalid", "Invalid"))`.
    - Assert 400 ProblemDetails with expected shape.
  - Note: Real FluentValidation won’t run in contract tests; simulate failures via canned Result as above.

- OpenAPI minimal check (optional)
  - Assert the above routes exist in Swagger if that’s covered by your existing swagger contract tests.

### Functional tests (application handlers + validators; no HTTP assertions)
Place under `tests/Application.FunctionalTests/Features/Public/Restaurants/` and use the `Testing` facade (SendAsync, AddAsync/FindAsync, TestDataFactory):

- GetFullMenuQuery handler:
  - Success path:
    - Arrange: Insert a `FullMenuView` row for a restaurant with known `MenuJson` and `LastRebuiltAt` using `AddAsync`.
    - Act: `await SendAsync(new GetFullMenuQuery(restaurantId))`.
    - Assert: `result.IsSuccess` and values match seeded `MenuJson` and `LastRebuiltAt`.
    - Content fidelity: seed `MenuJson` with pretty-printed/whitespace JSON and Unicode characters; assert the returned `MenuJson` string matches exactly (no re-serialization or trimming).
    - Offset preservation: seed `LastRebuiltAt` with a non-UTC offset (e.g., `+07:00`); assert the returned `DateTimeOffset` equals the seeded value including offset.
    - Multi-row safety: seed additional restaurants with their own `FullMenuView` rows; assert querying one `restaurantId` returns only its row.
    - Empty content (current behavior): seed an empty string for `MenuJson`; assert success and returned `MenuJson` is empty (handler does not parse/validate JSON).
  - Not found:
    - Arrange: Ensure no `FullMenuView` for a random restaurant.
    - Act: send query.
    - Assert: `IsFailure` with `GetFullMenuErrors.NotFound`.
  - Validation:
    - Act: send query with `Guid.Empty`.
    - Assert: `ValidationException` is thrown (per pipeline behavior).

- GetRestaurantPublicInfoQuery handler:
  - Success path:
    - Arrange: Insert `Restaurant` with `IsDeleted=false` and `CuisineTags` JSON (e.g., ["Vegan", "Italian"]).
    - Act: send query.
    - Assert: `IsSuccess` and DTO fields match; `CuisineTags` parsed to list.
  - Not found (deleted or missing):
    - Arrange: set `IsDeleted=true` or no row.
    - Act/Assert: failure with `GetRestaurantPublicInfoErrors.NotFound`.
  - Validation: empty `RestaurantId` → `ValidationException`.

- SearchRestaurantsQuery handler:
  - Text filter:
    - Arrange: Insert multiple restaurants with varying `Name`.
    - Act: send with `q` substring.
    - Assert: results include only matches; ordering by Name asc then Id asc is deterministic.
  - Cuisine filter:
    - Arrange: `CuisineTags` JSONB contains target tag in some rows.
    - Act: send with `cuisine`.
    - Assert: only tagged rows returned.
  - Pagination:
    - Arrange: insert > page size rows.
    - Act: request `pageNumber`/`pageSize`.
    - Assert: page metadata and item count consistent; ordering stable across pages.
  - Validator scenarios:
    - `pageSize > 50` or `< 1`, `pageNumber < 1` → `ValidationException`.
    - Geo coherence: providing only one of `lat/lng/radiusKm` → `ValidationException`; out-of-range lat/lng or `radiusKm > 25` → `ValidationException`.

- RebuildFullMenuCommand handler (admin command):
  - Arrange: Seed authoritative tables for a restaurant (enabled Menu, Categories with orders, Items, CustomizationGroups/Options, Tags) using factories/helpers.
  - Act: `await SendAsync(new RebuildFullMenuCommand(restaurantId))`.
  - Assert: command is successful; `FullMenuView` row exists with non-empty `MenuJson` and `LastRebuiltAt` within recent time; optionally assert minimal invariants in JSON (e.g., `version:1`, category order present) by deserializing into a lightweight anonymous type.
  - Idempotency: invoke command twice; assert single row logically updated and `LastRebuiltAt` moves forward.

### Key considerations and gaps to watch
- ETag format: Plan specifies `W/"..."`; `HttpCaching.BuildWeakEtag` currently yields `W"..."`. Decide whether to write the contract test to the plan (preferred) and fix helper, or codify current behavior and update the plan. Functional tests will also surface this.
- 412 on invalid conditional headers: Plan mentions 412; current code treats bad headers as “no cache” (no 412). If 412 is desired, add tests marked “expected (spec)” and “current behavior” to guide a fix.
- Content-type: Endpoint sets `application/json` explicitly; assert starts with `application/json` to avoid brittleness if charset is appended by hosting.
- ProblemDetails mapping: Ensure `Error.Code` first segment drives `title` (e.g., `Public.*`). Use correct typed `Result<T>` in contract tests per guidelines.
- Strongly typed IDs: Public DTOs use primitive GUIDs; contract tests should assert primitives, not VO wrappers.
- Pagination helper: Verify deterministic ordering in integration/functional tests; avoid over-asserting fields not yet implemented (e.g., ratings are stubbed to null).
- Rate limiting/Vary headers: Not implemented in code; don’t assert unless added.
- Admin endpoint presence: If endpoint is missing, cover handler/integration tests; add endpoint contract/functional tests when it’s added.

### Proposed test file map (concise)
- Contract
  - `Restaurants/MenuContractTests.cs`
  - `Restaurants/InfoContractTests.cs`
  - `Restaurants/SearchContractTests.cs`
- Functional
  - `Features/Restaurants/MenuEndpointTests.cs`
  - `Features/Restaurants/InfoEndpointTests.cs`
  - `Features/Restaurants/SearchEndpointTests.cs`
  - `Features/Admin/RebuildFullMenuTests.cs` (command only)
