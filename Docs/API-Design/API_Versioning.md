## API Versioning Guide

The YummyZoom API is versioned to allow for evolution without breaking existing client applications. This guide outlines the principles and procedures for managing API versions.

### Guiding Principles

1.  **Each Version is a Complete Contract:** A client should be able to use the application's full functionality by targeting a single API version (e.g., `v2`). They should **never** have to mix calls to `v1` and `v2`.
2.  **Create New Versions for Breaking Changes Only:** A new version (e.g., `v2`) is only justified when a breaking change is required. Non-breaking changes should be added to the current latest version.
3.  **Promote, Don't Abandon:** When a new version is created, any stable, unchanged endpoints from the previous version **must** be explicitly promoted to the new version's contract.
4.  **URL Convention:** We use URI Path Versioning with kebab-case for resource names (e.g., `/api/v1/todo-lists`). This is handled automatically by the `MapGroup` extension method.

---

### Scenario 1: Adding a New Endpoint (Non-Breaking Change)

This is the most common scenario. You are adding new functionality without changing existing endpoints.

**Procedure:**
Add the new endpoint to the **latest existing API version**.

**Example:** Assume the latest version is `v1.0`. We are adding a new `Reviews` endpoint.

1.  Create the new endpoint class `src\Web\Endpoints\Reviews.cs`.
2.  In the `Map` method, explicitly map the group to the latest version (`1.0`).

```csharp
// In src\Web\Endpoints\Reviews.cs
public class Reviews : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization()
            .MapToApiVersion(1, 0); // Map to the latest version

        group.MapGet(GetReviews);
        // ... other endpoints ...
    }
    // ... handler methods ...
}
```

---

### Scenario 2: Introducing a Breaking Change (Creating a New Version)

This is the process for evolving an existing endpoint in a way that would break clients.

**Definition of a Breaking Change:**
*   Adding a required field to a request.
*   Removing a field from a response.
*   Changing a field's data type.
*   Renaming a field.
*   Changing a URL or HTTP method.

**Procedure:**
1.  Announce the new API version in the application.
2.  Implement the new version of the changed endpoint.
3.  Promote all stable, unchanged endpoints to the new version.

**Example:** Assume we need to make a breaking change to `TodoLists`. We will create `v2.0`.

**Step 1: Announce `v2.0`**

Modify `src\Web\Infrastructure\WebApplicationExtensions.cs` to include the new version.

```csharp
// In src\Web\Infrastructure\WebApplicationExtensions.cs
public static WebApplication MapVersionedEndpoints(this WebApplication app)
{
    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .HasApiVersion(new ApiVersion(2, 0)) // <-- Announce the new version here
        .ReportApiVersions()
        .Build();
    // ... rest of the method
}
```

**Step 2: Implement the Changed Endpoint**

In the endpoint file for the feature being changed (e.g., `src\Web\Endpoints\TodoLists.cs`), add a new mapping for `v2.0` that points to a new handler with the new logic.

```csharp
// In src\Web\Endpoints\TodoLists.cs
public override void Map(IEndpointRouteBuilder app)
{
    // === V1 Endpoints ===
    var groupV1 = app.MapGroup(this)
        .RequireAuthorization()
        .MapToApiVersion(1, 0); // Original endpoints belong to v1
    groupV1.MapGet(GetTodoListsV1);

    // === V2 Endpoints ===
    var groupV2 = app.MapGroup(this)
        .RequireAuthorization()
        .MapToApiVersion(2, 0); // New endpoint with breaking change belongs to v2
    groupV2.MapGet(GetTodoListsV2); // This handler uses the new V2 Command/DTO
}

private async Task<IResult> GetTodoListsV1(ISender sender) { /* ... v1 logic ... */ }
private async Task<IResult> GetTodoListsV2(ISender sender) { /* ... v2 logic ... */ }
```

**Step 3: Promote Unchanged Endpoints**

For every other existing endpoint group (e.g., `Users`, `WeatherForecasts`) that is still required, you **must** map it to `v2.0` to ensure `v2` is a complete contract.

```csharp
// In src\Web\Endpoints\Users.cs
public override void Map(IEndpointRouteBuilder app)
{
    var group = app.MapGroup(this)
        .MapToApiVersion(1, 0)   // <-- Still part of v1
        .MapToApiVersion(2, 0);  // <-- Also promoted to v2

    // The handler is the same, no code duplication needed.
    group.MapIdentityApi<ApplicationUser>();
    // ...
}
```

---

### Scenario 3: Deprecating an Old API Version

When you are ready to encourage clients to move off an old version, you can mark it as deprecated.

**Procedure:**
In `src\Web\Infrastructure\WebApplicationExtensions.cs`, add the `"deprecated"` status to the version you wish to phase out.

```csharp
// In src\Web\Infrastructure\WebApplicationExtensions.cs
public static WebApplication MapVersionedEndpoints(this WebApplication app)
{
    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0), "deprecated") // <-- Mark v1 as deprecated
        .HasApiVersion(new ApiVersion(2, 0))
        .ReportApiVersions()
        .Build();
    // ...
}
```
This will add a `api-deprecated-versions` header to responses and mark the version as deprecated in the Swagger UI, signaling to clients that they should upgrade.
