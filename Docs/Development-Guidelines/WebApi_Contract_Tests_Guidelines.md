# Web API Contract Tests Guidelines

## 1. Purpose & Scope
The `Web.ApiContractTests` project provides **fast, deterministic HTTP boundary tests** that lock down the externally visible API contract (routes, verbs, status codes, JSON schema shapes, auth behaviors, and error envelopes) **without executing real domain/application logic, persistence, or pipeline behaviors**.

These tests answer: *"Given request X to endpoint Y, do we return the expected HTTP status / shape / envelope?"*

They deliberately **stub MediatR handlers** and **simulate authentication** to isolate only the *API surface contract*. This keeps feedback near-instant (sub‑second per test) and reduces brittleness.

## 2. Goals / Non-Goals
| Goals | Non-Goals |
|-------|-----------|
| Verify routing, version segments, parameter binding | Execute real DB / repository logic |
| Verify request → command/query mapping shape | Exercise complex business rules |
| Verify success & error status codes and ProblemDetails format | Replace functional/integration tests |
| Verify JSON serialization of strongly typed IDs and value objects | Test performance characteristics |
| Verify OpenAPI document contains required elements (e.g. JWT security scheme) | Full schema diffing (handled by other tooling if needed) |

## 3. Architectural Overview
Key layers involved in contract tests:
- ASP.NET Core minimal API endpoints (in `src/Web/Endpoints/*`)
- Custom result mapping (`ResultExtensions`, `CustomResults`) to `ProblemDetails`
- MediatR `ISender` replaced by a `CapturingSender` test double
- Test auth scheme replaces real bearer/JWT auth
- Shared JSON serialization configuration extended to include strongly-typed ID converter

## 4. Test Project Structure
```
Web.ApiContractTests/
  Infrastructure/
    ApiContractWebAppFactory.cs   (custom factory)
    CapturingSender.cs            (MediatR test double)
    TestAuthHandler.cs            (fake auth)
  Orders/
    InitiateOrderContractTests.cs
    StatusContractTests.cs
    AuthContractTests.cs
  OpenApi/
    SwaggerContractTests.cs
```

## 5. Core Infrastructure Components
### ApiContractWebAppFactory
- Builds the Web host with environment "Test"
- Replaces `ISender` with `CapturingSender`
- Injects test auth handler & disables real background services
- Adds a dummy connection string (ensures EF or services expecting it don't crash)

### CapturingSender
- Captures the last MediatR request (`LastRequest`) for assertion
- Supplies canned responses via `RespondWith(Func<object, object?>)`
- Implements required `Send` overloads for both generic and non-generic MediatR requests

### TestAuthHandler
- Recognizes custom headers (`x-test-user-id`, optional permissions header)
- Produces either authenticated or 401 responses deterministically
- Eliminates dependencies on token generation / validation

## 6. Test Authoring Pattern
Each contract test follows a consistent shape:
1. **Arrange**
   - Create factory & client
   - Set headers (`x-test-user-id` when auth is needed)
   - Configure `factory.Sender.RespondWith(...)` to produce the desired `Result<T>` or `Result.Failure<T>`
2. **Act**
   - Issue HTTP request using real route & versioning (e.g. `/api/v1/orders/initiate`)
3. **Assert**
   - Status code
   - Response body shape / critical fields / ProblemDetails semantics
   - Optionally: inspect `factory.Sender.LastRequest` to ensure mapping is correct
4. **Log (human aid)**
   - Print serialized request & raw response to `TestContext.WriteLine` for manual review if needed.

## 7. Handling MediatR Requests
Return **typed** results that match the endpoint’s expected generic `Result<T>` signature. Example:
```csharp
factory.Sender.RespondWith(_ => Result.Failure<GetOrderByIdResponse>(Error.NotFound("Order.NotFound", "Missing")));
```
Avoid returning non-generic `Result.Failure(...)` for a generic endpoint—this causes casting issues inside the sender.

## 8. Authentication Simulation
- Provide `x-test-user-id` header to simulate an authenticated principal
- Omit header to assert `401 Unauthorized`
- (Optional extension) Add permissions header (e.g. `x-test-permissions`) to simulate policy-based checks

## 9. Strongly Typed ID Serialization
The production code uses value objects (e.g. `OrderId : AggregateRootId<Guid>`). To keep external JSON stable (primitive GUIDs rather than `{ "value": "..." }` objects):
- `DependencyInjection.AddWebServices` registers `AggregateRootIdJsonConverterFactory` via `ConfigureHttpJsonOptions`
- Tests deserialize using the same `DomainJson.Options` for parity

If a new strongly typed ID is added, the converter handles it automatically—no additional test changes needed.

## 10. Request & Response Logging
Tests log:
- Request line + serialized JSON body (if applicable)
- Response status & raw JSON
Use `TestContext.WriteLine`. Keep logs concise—avoid dumping huge payloads. This aids PR review and human contract inspection.

## 11. Adding a New Endpoint Contract Test (Checklist)
- [ ] Identify route & versioned path (`/api/v1/...`)
- [ ] Determine expected success status + body root shape
- [ ] Determine failure scenario(s) (e.g. NotFound, Validation)
- [ ] Decide minimal canned response object (construct DTO or `Result.Failure<T>`)—do **not** involve real data access
- [ ] Configure `RespondWith` to validate mapping & emit desired result
- [ ] Send HTTP request with appropriate method & body
- [ ] Assert status code
- [ ] Assert key JSON fields (avoid over-asserting incidental fields unless contractually critical)
- [ ] Assert mapping by inspecting `factory.Sender.LastRequest` (type + selected properties)
- [ ] Log request & response

## 12. Result & ProblemDetails Mapping
The pipeline converts `Result`/`Result<T>` failures to `ProblemDetails` using:
- `ErrorType.NotFound` → 404
- `ErrorType.Validation` / `ErrorType.Failure` → 400
- `ErrorType.Conflict` → 409
- Others → 500
Tests verify:
- `status` matches
- `title` derived from first segment of `Error.Code` (e.g. `Order.NotFound` → `Order`)

## 13. Naming Conventions
- File: `<Feature><Entity>ContractTests.cs` or specific scenario grouping (e.g. `AuthContractTests.cs`)
- Test: `<EndpointAction>_<Precondition>_<ExpectedOutcome>`
  - Example: `GetOrderById_WhenNotFound_Returns404Problem`

## 14. Common Pitfalls & Anti-Patterns
| Pitfall | Remedy |
|---------|--------|
| Returning non-generic `Result.Failure` for a generic MediatR endpoint | Use `Result.Failure<T>` with correct `T` |
| Over-asserting every response field | Assert only contractually relevant fields |
| Using real DB / seeding logic | Avoid; use canned DTO construction |
| Integrating real auth flows | Use headers + test auth handler |
| Manual parsing of strongly typed IDs | Prefer converter registration; deserialize typed DTOs |

## 15. Performance Tips
- Keep tests focused—one assertion theme per test
- Reuse `ApiContractWebAppFactory` per test when test isolation allows (current pattern creates per test for simplicity)
- Avoid large payload generation; minimal valid objects only

## 16. When to Use Each Test Type
| Scenario | Contract Test | Functional Test | Integration Test |
|----------|---------------|-----------------|------------------|
| Verify route + status code mapping | ✅ | ❌ | ❌ |
| Verify domain rule & side effects | ❌ | ✅ | ✅ |
| Verify DB persistence / EF mapping | ❌ | ❌ | ✅ |
| Verify OpenAPI presence | ✅ | ❌ | ❌ |
| Regression of JSON envelope shape | ✅ | ✅ (if broad) | ❌ |

## 17. Future Enhancements
- Schema snapshot/diff (optional tool) to detect unintentional breaking changes
- 403 Forbidden coverage using permissions header
- Validation error scenario (e.g. invalid GUID binding or FluentValidation error) to lock down 400 envelope
- Automatic generation of contract test skeletons from endpoint metadata
- Optional toggle to suppress request/response logging except on failure

## 18. Quick Example (Pattern Recap)
```csharp
[Test]
public async Task ExampleEndpoint_WhenDomainReturnsNotFound_Returns404()
{
    var factory = new ApiContractWebAppFactory();
    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

    factory.Sender.RespondWith(_ => Result.Failure<MyResponseDto>(Error.NotFound("Resource.NotFound", "Missing")));

    var path = "/api/v1/resources/" + Guid.NewGuid();
    TestContext.WriteLine($"REQUEST GET {path}");
    var resp = await client.GetAsync(path);

    var raw = await resp.Content.ReadAsStringAsync();
    TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

    resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
    prob!.Title.Should().Be("Resource");
}
```

## 19. Summary
Contract tests give **high-signal, low-noise protection** over the HTTP façade. Keep them lean, deterministic, and focused on externally observable behavior. Push any logic-heavy concerns to functional / integration tests. Always ensure typed `Result<T>` correctness, consistent serialization, and minimal but meaningful assertions.
