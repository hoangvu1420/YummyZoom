# YummyZoom Web API Contract Test Project – Lean Design (Revised)

This revision aligns the original lean contract test concept with the **current repository patterns** (central package management, .NET 9, versioned endpoints, custom ProblemDetails, authorization policies, hosted services). It keeps focus strictly at the HTTP boundary while avoiding overlap with existing functional / integration tests.

---

## Goals (Confirmed)

* ✅ Verify endpoint **routing and versioning** (segment style: `/api/v1.0/...` for requests; OpenAPI doc at `/api/v1/specification.json`).
* ✅ Ensure **request → command mapping** and **command → response mapping** surface (captured via test double, not by invoking handlers).
* ✅ Confirm **status codes** and **ProblemDetails formatting** (shape produced by `CustomResults`).
* ✅ Validate **auth enforcement at boundary** (`401` for missing principal; optionally `403` for forbidden when authenticated without required permission claim).
* ✅ Provide **fast feedback** independent of database, migrations, background hosted services.
* ❌ Do **not** re‑exercise business logic, database persistence, MediatR pipeline behaviors (validation, authorization, logging, performance), or EF Core mapping.

### Non-Goals
* ❌ Testing MediatR pipeline behaviors (Validation / Authorization) – those are bypassed intentionally by replacing `ISender`.
* ❌ End-to-end data state assertions – handled by functional tests with real DB + Testcontainers.
* ❌ Re-validating OpenAPI generation exhaustively – only smoke / key security scheme & version path.
* ❌ Load / performance benchmarking.

---

## Project Structure (Simplified)

```
/tests
├── Web.ApiContractTests/            # NEW PROJECT (this proposal)
│   ├── Web.ApiContractTests.csproj
│   ├── Infrastructure/
│   │   ├── ApiContractWebAppFactory.cs
│   │   ├── CapturingSender.cs       # ISender test double
│   │   └── TestAuthHandler.cs       # Fake auth handler
│   ├── Orders/
│   │   ├── InitiateOrderContractTests.cs
│   │   ├── StatusContractTests.cs
│   │   └── AuthContractTests.cs
│   └── OpenApi/
│       └── SwaggerContractTests.cs (optional)
```

---

## Dependencies & Package Strategy

Use **central package management** (already enabled via `Directory.Packages.props`). The test project should list *unversioned* `<PackageReference />` items consistent with existing test projects.

Required:
* Microsoft.AspNetCore.Mvc.Testing – in-memory host
* Microsoft.NET.Test.Sdk – test discovery (implicit in other test projects)
* nunit / NUnit3TestAdapter / NUnit.Analyzers – framework & analyzers
* coverlet.collector – coverage (parity with other test projects)
* FluentAssertions – expressive assertions
* (Optional) System.Net.Http.Json is part of the shared framework; no explicit reference needed on .NET 9.

No explicit versions should be specified locally.

---

## Key Infrastructure

### `CapturingSender.cs`

Test double replacing `ISender` to (a) capture the *most recent* MediatR request, (b) return canned responses without invoking real handlers, (c) implement the full `ISender` surface (including stream and non-generic overloads) to avoid interface drift risk.

```csharp
public sealed class CapturingSender : ISender
{
    public object? LastRequest { get; private set; }
    private Func<object, object?>? _responder;

    public void RespondWith(Func<object, object?> responder) => _responder = responder;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        LastRequest = request;
        var value = _responder is null ? default : _responder(request);
        return Task.FromResult((TResponse?)value!);
    }

    public Task<object?> Send(object request, CancellationToken ct = default)
    {
        LastRequest = request;
        var value = _responder?.Invoke(request);
        return Task.FromResult(value);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
    {
        LastRequest = request;
        return AsyncEnumerable.Empty<TResponse>();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
    {
        LastRequest = request;
        return AsyncEnumerable.Empty<object?>();
    }
}
```

Usage pattern in tests:
```csharp
factory.Sender.RespondWith(req => Result.Success(new SomeResponse(...)));
factory.Sender.LastRequest.Should().BeOfType<ExpectedCommand>();
```

### `TestAuthHandler.cs`

Header-driven authentication:
* `x-test-user-id` – establishes an authenticated principal (NameIdentifier claim).
* (Optional extension) `x-test-permissions` – semicolon-delimited permission claims (e.g. `RestaurantOwner:rest-1;UserOwner:user-1`) to enable a **403** scenario when missing required permissions.

```csharp
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["x-test-user-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.Fail("No user"));

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### `ApiContractWebAppFactory.cs`

Configures:
1. Dummy connection string (avoids guard exception in `AddInfrastructureServices`).
2. Replacement of `ISender` with `CapturingSender`.
3. Test authentication scheme replacement **after** normal service registration via `ConfigureTestServices`.
4. Disables the Outbox hosted service (mirrors functional test factory) to prevent background noise.
5. Sets environment to `Test` so database initialisation (Development-only) is skipped.

```csharp
public class ApiContractWebAppFactory : WebApplicationFactory<Program>
{
    public CapturingSender Sender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:YummyZoomDb", "Host=localhost;Database=dummy;Username=dummy;Password=dummy");

        builder.ConfigureTestServices(services =>
        {
            // Replace ISender
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(ISender));
            if (existing is not null) services.Remove(existing);
            services.AddSingleton<ISender>(Sender);

            // Disable Outbox publisher hosted service
            var hosted = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "OutboxPublisherHostedService");
            if (hosted is not null) services.Remove(hosted);

            // Inject test auth
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }
}
```

---

## Example Tests

### `InitiateOrderContractTests.cs`

```csharp
public class InitiateOrderContractTests
{
    [Test]
    public async Task InitiateOrder_MapsRequestToCommand_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client  = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<InitiateOrderCommand>();
            return Result.Success(new InitiateOrderResponse(expectedId));
        });

        var body = new InitiateOrderRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            new() { new InitiateOrderItemRequest(Guid.NewGuid(), 1) },
            new("Street", "City", "State", "Zip", "Country"),
            "card", null, null, 0m, null);

    var resp = await client.PostAsJsonAsync("/api/v1.0/orders/initiate", body); // versioned route
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<InitiateOrderResponse>();
        dto!.OrderId.Should().Be(expectedId);
    }
}
```

### `StatusContractTests.cs`

```csharp
public class StatusContractTests
{
    [Test]
    public async Task GetOrderById_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(_ => Result<GetOrderByIdResponse>.Failure(Error.NotFound("Order.NotFound", "Missing order")));

    var resp = await client.GetAsync($"/api/v1.0/orders/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(404);
    }
}
```

### `AuthContractTests.cs`

```csharp
public class AuthContractTests
{
    [Test]
    public async Task InitiateOrder_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

    var resp = await client.PostAsJsonAsync("/api/v1.0/orders/initiate", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## Optional Swagger Test

```csharp
[Test]
public async Task Swagger_IncludesJwtScheme()
{
    var factory = new ApiContractWebAppFactory();
    var client  = factory.CreateClient();

    var resp = await client.GetAsync("/api/v1/specification.json"); // documentName pattern uses v1
    resp.IsSuccessStatusCode.Should().BeTrue();
    var json = await resp.Content.ReadAsStringAsync();
    json.Should().Contain("JWT");
}
```

---

## `Web.ApiContractTests.csproj`

Target **.NET 9** (matches `global.json`). No explicit versions (resolved centrally).

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <RootNamespace>YummyZoom.Web.ApiContractTests</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="nunit" />
        <PackageReference Include="NUnit3TestAdapter" />
        <PackageReference Include="NUnit.Analyzers" />
        <PackageReference Include="coverlet.collector" />
        <PackageReference Include="FluentAssertions" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\src\Web\Web.csproj" />
    </ItemGroup>
</Project>
```

`System.Net.Http.Json` is part of the shared framework; no separate reference needed.

## Additional / Optional Tests
* 403 Scenario: Authenticate a user (x-test-user-id) but omit required permission claim (if a policy-protected endpoint is exposed). Expect `403`.
* Invalid Route Parameter: `GET /api/v1.0/orders/not-a-guid` → 400 (model binding) ensures basic parameter validation surfacing.
* ProblemDetails Shape: For simulated validation error via `RespondWith(_ => Result.Failure(Error.Validation("Order.InvalidState", "...")))` assert Title == `Order` and Status 400.
* OpenAPI Security Scheme: Parse JSON and assert `components.securitySchemes.JWT` exists (optional robustness beyond string contains).

## Known Trade-offs & Rationale
| Aspect | Decision | Rationale |
|--------|----------|-----------|
| MediatR pipeline | Bypassed | Speed; functional tests cover behaviors |
| DB / EF Core | Not initialized | Guard passed via dummy connection + no handler execution |
| Hosted Services | Outbox disabled | Removes nondeterministic background activity |
| Auth depth | Header-only | Only boundary status codes needed |
| OpenAPI coverage | Smoke test | Full diffing is brittle & higher maintenance |

## Execution Notes
* Use `ApiContractWebAppFactory` per test (cheap) or share instance if desired; state limited to last request captured.
* Always set `x-test-user-id` when asserting authorized flows.
* When asserting mapping, inspect `factory.Sender.LastRequest` before reading the HTTP response body (both are synchronous in effect but clarity helps).
* Prefer resilient assertions (e.g. ProblemDetails `Title` & `Status` rather than entire JSON).

## Summary

* **Fast**: No database, no containers, no pipeline overhead.
* **Focused**: Validates only the HTTP contract (route, version, auth boundary, mapping, error surface, OpenAPI presence).
* **Consistent**: Aligns with repository conventions (central packages, .NET 9, version path, ProblemDetails style).
* **Extensible**: Easy to add per-endpoint mapping tests without increasing runtime cost.
* **Complementary**: Leaves business logic, persistence, validation & authorization behaviors to existing functional/integration test suites.

This refined design is ready for project scaffolding.

---

## Summary

* **Fast**: no DB, no containers.
* **Focused**: only the HTTP contract.
* **Auth**: tested at boundary (401/403), not in business logic.
* **Mapping**: verified by capturing the command sent to MediatR.
* **Errors**: validated via ProblemDetails.

This keeps API contract tests lean and complementary to functional tests.
