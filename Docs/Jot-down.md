Universal Search 400 Issue – Context Pack

Environment
- .NET SDK: 9.0.302; VSTest: 17.14.1 (x64); OS: Windows 10 (win-x64)
- Test project: `tests/Web.ApiContractTests`
- Web project: `src/Web`
- Versioning: API routes are under `/api/v{version:apiVersion}`; default v1 configured

Endpoint Definition (Search)
- Group mapping: `src/Web/Infrastructure/WebApplicationExtensions.cs:21` builds versioned group, then iterates endpoint groups (subclasses of `EndpointGroupBase`) and invokes `Map(versionedGroup)`.
- Search endpoints: `src/Web/Endpoints/Search.cs:1`
  - Group base path resolved from class name via `MapGroup(this)` → `/search` inside `/api/v1`.
  - Universal Search endpoint (root of group):
    - Method: GET
    - Path: `/api/v1/search` (handler mapped with empty pattern string)
    - Signature: `async ([AsParameters] UniversalSearchRequestDto req, ISender sender)`
    - Behavior: constructs `UniversalSearchQuery` and `await sender.Send(rq)`, returns `Results.Ok` on success, else `res.ToIResult()`.
  - Autocomplete endpoint:
    - Method: GET
    - Path: `/api/v1/search/autocomplete`
    - Signature: `async ([AsParameters] AutocompleteRequestDto req, ISender sender)`
    - Behavior: `sender.Send(new AutocompleteQuery(req.Term))` then OK or `ToIResult()`.

Request DTOs
- `UniversalSearchRequestDto` (query-bound):
  - Properties: `string? Term`, `double? Lat`, `double? Lon`, `bool? OpenNow`, `string[]? Cuisines`, `string[]? Tags`, `short[]? PriceBands`, `bool IncludeFacets = false`, `int PageNumber = 1`, `int PageSize = 10`.
- `AutocompleteRequestDto` (query-bound): `string Term { get; init; } = string.Empty;`

Application Query & Validation
- Universal search MediatR request and handler: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:1`.
  - Request record `UniversalSearchQuery` accepts the DTO fields (some renamed e.g., `Latitude/Longitude`).
  - Handler runs Dapper SQL and returns `Result<UniversalSearchResponseDto>`.
  - Facets optional via `IncludeFacets`.
- Validation: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryValidator.cs:1`.
  - Rules: `PageNumber >= 1`, `PageSize in [1,100]`, `Term <= 256`, `Latitude ∈ [-90,90]` if set, `Longitude ∈ [-180,180]` if set, lengths for `Cuisines/Tags`, non-negative `PriceBands` when present.

Result → HTTP Mapping
- `src/Web/Infrastructure/ResultExtensions.cs:1`: `Result<T>.ToIResult()` routes to `CustomResults.Problem` for failures.
- `src/Web/Infrastructure/CustomResults.cs:1`: Maps `ErrorType.Validation → 400`, `NotFound → 404`, `Conflict → 409`, `Failure → 400`, else `500`, returning `ProblemDetails` body.
- Global API behavior: `src/Web/DependencyInjection.cs:1` sets `options.SuppressModelStateInvalidFilter = true` and registers `AddExceptionHandler<CustomExceptionHandler>()`.

Test Infrastructure (Contract Tests)
- Factory: `tests/Web.ApiContractTests/Infrastructure/ApiContractWebAppFactory.cs:1`
  - Replaces `ISender` with `CapturingSender` to stub MediatR results.
  - Injects test auth scheme (public endpoints omit auth headers; results in anonymous access).
  - Disables a hosted service (`OutboxPublisherHostedService`).
  - Adds `AddProblemDetails()` for tests.
  - Sets client `BaseAddress` to `https://localhost` to bypass HTTP→HTTPS redirection in TestServer.
  - Configures `HttpsRedirectionOptions.HttpsPort = 443` and `builder.UseSetting("https_port","443")` to satisfy `HttpsRedirectionMiddleware` when present.
- CapturingSender: `tests/Web.ApiContractTests/Infrastructure/CapturingSender.cs:1` captures last request and returns canned `Result<T>`.
- Test auth: `tests/Web.ApiContractTests/Infrastructure/TestAuthHandler.cs:1` honors `x-test-user-id` (not required for public endpoints).

The Failing Tests
- File: `tests/Web.ApiContractTests/Search/UniversalSearchContractTests.cs:1`
  - Test 1: `UniversalSearch_WithMinimalParams_Returns200_WithPageAndEmptyFacets`
    - Request: `GET /api/v1/search`
    - Stubbed sender returns `Result.Success(new UniversalSearchResponseDto(...))`.
    - Expected: 200 OK with page info and empty facets.
    - Actual: 400 BadRequest, empty body (Content-Length: 0), Content-Type empty.
  - Test 2: `UniversalSearch_WhenValidationFails_Returns400Problem`
    - Request: `GET /api/v1/search?pageNumber=0&pageSize=1000` (intentionally “invalid” but failure is produced by the stub)
    - Stubbed sender returns `Result.Failure<UniversalSearchResponseDto>(Error.Validation("Search.Invalid", "Bad query"))`.
    - Expected: 400 with ProblemDetails (title "Search").
    - Actual: 400 with empty body (Content-Length: 0), Content-Type empty.

Contrast: Passing Tests
- Autocomplete: `tests/Web.ApiContractTests/Search/AutocompleteContractTests.cs:1`
  - `GET /api/v1/search/autocomplete?term=piz` → 200 with JSON array body (works).
- Restaurants public info: `tests/Web.ApiContractTests/Restaurants/InfoContractTests.cs:1`
  - `GET /api/v1/restaurants/{id}/info` → 200 and 404 scenarios both work with expected JSON/ProblemDetails bodies.

Repro Commands (used during diagnosis)
- Minimal failing test filter:
  - `dotnet test tests/Web.ApiContractTests/Web.ApiContractTests.csproj -c Release --filter "FullyQualifiedName~UniversalSearch_WhenValidationFails_Returns400Problem" -v minimal`
  - `dotnet test tests/Web.ApiContractTests/Web.ApiContractTests.csproj -c Release --filter "FullyQualifiedName~UniversalSearch_WithMinimalParams_Returns200_WithPageAndEmptyFacets" -v minimal`
- Passing reference tests:
  - `dotnet test ... --filter "FullyQualifiedName~Autocomplete_WithValidTerm_Returns200_WithSuggestions"`
  - `dotnet test ... --filter "FullyQualifiedName~Restaurants.InfoContractTests.GetRestaurantInfo_WhenFound_Returns200WithDtoShape"`

Observed Runtime Logs (from failing runs)
- Prior to adjustments, saw: `Failed to determine the https port for redirect.` (HttpsRedirectionMiddleware). After setting BaseAddress to https and configuring https_port, the warning is no longer present for these specific runs.
- Response details consistently show: `RESPONSE 400 BadRequest` with `Content Length: 0` and no content type for the universal search endpoint.

Key Observations
- The 400 response for `/api/v1/search` appears to be generated before the endpoint handler executes (empty body, no ProblemDetails mapping, despite stubbing `Result<T>`). CapturingSender likely not invoked for these requests.
- `/api/v1/search/autocomplete` works with the same group, DI, and sender, so group routing and DI are generally correct.
- Other unrelated endpoints (e.g., restaurants info) also work; versioning configuration seems fine.
- The universal search handler uses `[AsParameters] UniversalSearchRequestDto` (record with many optional properties and defaults). Minimal API binding may be failing early and returning a bare 400.
- Changing route pattern from `"/"` to `""` did not change behavior (still 400, empty body).

Hypotheses (to validate with external sources)
- Minimal API [AsParameters] binding edge-case: When binding a record/class with optional arrays and non-nullable primitives with defaults, binder might be failing for the empty query case, causing a 400 with no body.
- A specific property in `UniversalSearchRequestDto` could be causing binder failure when omitted (e.g., `IncludeFacets` or numeric defaults), unlike `AutocompleteRequestDto` which only binds a single string.
- Route mapping quirk for the group root handler (empty pattern) interacting with versioned groups. However, other groups/handlers at non-root paths behave correctly and versioning is working.
- Some implicit model binding failure without ModelState details because `SuppressModelStateInvalidFilter = true` and this is minimal APIs (not MVC controllers), leading to 400 with no body before the handler.

Potential Next Experiments (non-exhaustive)
- Replace `[AsParameters] UniversalSearchRequestDto` with explicit `[FromQuery]` parameters on the handler to see if binding succeeds and the stubbed sender is hit.
- Alternatively, keep `[AsParameters]` but make all properties nullable (including `IncludeFacets`, `PageNumber`, `PageSize`) to observe if binder stops failing; then set defaults in code when constructing the query.
- Add a simple "ping" MapGet at `/api/v1/search/__probe` within the same group to verify the group base path isn’t misbehaving.
- Add test-time logging middleware to write when the handler is hit, or assert `factory.Sender.LastRequest` after call to confirm if the handler is reached.
- Temporarily remove `[AsParameters]` and bind a single dummy `string? Term` parameter to see if `/api/v1/search` starts returning 200 with stubbed response.

Relevant Code References
- Endpoint: `src/Web/Endpoints/Search.cs:1`
- Grouping/versioning: `src/Web/Infrastructure/WebApplicationExtensions.cs:21`
- Request DTO: `src/Web/Endpoints/Search.cs:44`
- UniversalSearch query/handler: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:1`
- UniversalSearch validator: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryValidator.cs:1`
- Result mapping: `src/Web/Infrastructure/ResultExtensions.cs:1`, `src/Web/Infrastructure/CustomResults.cs:1`
- Global API behavior: `src/Web/DependencyInjection.cs:1`
- Test factory: `tests/Web.ApiContractTests/Infrastructure/ApiContractWebAppFactory.cs:1`
- Failing tests: `tests/Web.ApiContractTests/Search/UniversalSearchContractTests.cs:1`
- Passing search-autocomplete tests: `tests/Web.ApiContractTests/Search/AutocompleteContractTests.cs:1`

Summary
The universal search root endpoint (`GET /api/v1/search`) consistently returns 400 with an empty body under contract tests, while the autocomplete and other endpoints behave as expected. Evidence points to an early request binding failure (likely `[AsParameters]` with the complex DTO), occurring before the handler and before custom `Result`→`ProblemDetails` mapping. The next step is to narrow the binding surface (explicit `[FromQuery]` parameters or relaxed DTO nullability) to validate the binding hypothesis and guide a production-friendly fix.

