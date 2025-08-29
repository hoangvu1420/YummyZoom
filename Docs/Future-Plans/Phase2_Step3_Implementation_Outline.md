# Phase 2, Step 3: Implementation Outline
## Menu and Category Management Commands + Projector Extension

### Executive Summary

This document outlines the implementation plan for Phase 2, Step 3: "Expand to categories and menu-level commands; extend projector coverage." This phase builds upon the menu item management capabilities established in Step 2 to provide a complete menu hierarchy management experience for restaurant staff. All changes will be automatically synchronized to the public-facing `FullMenuView` read model.

### Current State Analysis

**✅ Completed in Step 1 & 2:**
- Public read infrastructure (`GetFullMenuQuery`, public endpoints).
- `FullMenuViewRebuilder` service for manual and event-driven rebuilds.
- Complete command and event handler suite for `MenuItem` management.
- `IMenuRepository`, `IMenuCategoryRepository`, and `IMenuItemRepository` are implemented and registered.
- Management endpoints for all `MenuItem` operations.
- Event-driven projector updates `FullMenuView` based on all `MenuItem` events.

**❌ Missing for Step 3:**
- Write commands for `Menu` and `MenuCategory` management.
- Event handlers (projectors) for `Menu` and `MenuCategory` events.
- Management endpoints for restaurant staff to manage menus and categories.

### Implementation Plan

#### Phase A: Core Menu & Category Commands (Days 1-3)

**A1: Menu Commands**

1.  **CreateMenuCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantOwner)]
    public record CreateMenuCommand(
        Guid RestaurantId,
        string Name,
        string Description,
        bool IsEnabled = true
    ) : IRequest<Result<CreateMenuResponse>>, IRestaurantCommand;
    ```

2.  **UpdateMenuDetailsCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantStaff)]
    public record UpdateMenuDetailsCommand(
        Guid RestaurantId,
        Guid MenuId,
        string Name,
        string Description
    ) : IRequest<Result>, IRestaurantCommand;
    ```

3.  **ChangeMenuAvailabilityCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantStaff)]
    public record ChangeMenuAvailabilityCommand(
        Guid RestaurantId,
        Guid MenuId,
        bool IsEnabled
    ) : IRequest<Result>, IRestaurantCommand;
    ```

**A2: Menu Category Commands**

1.  **AddMenuCategoryCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantStaff)]
    public record AddMenuCategoryCommand(
        Guid RestaurantId,
        Guid MenuId,
        string Name
    ) : IRequest<Result<AddMenuCategoryResponse>>, IRestaurantCommand;
    ```

2.  **UpdateMenuCategoryDetailsCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantStaff)]
    public record UpdateMenuCategoryDetailsCommand(
        Guid RestaurantId,
        Guid MenuCategoryId,
        string Name,
        int DisplayOrder
    ) : IRequest<Result>, IRestaurantCommand;
    ```

3.  **RemoveMenuCategoryCommand**
    ```csharp
    [Authorize(Policy = Policies.MustBeRestaurantStaff)]
    public record RemoveMenuCategoryCommand(
        Guid RestaurantId,
        Guid MenuCategoryId
    ) : IRequest<Result>, IRestaurantCommand;
    ```

**A3: Command Handlers & Validators**
- Implement handlers for each command, following the existing pattern: validate, load aggregate, execute domain operation, save.
- Create `FluentValidation` validators to enforce rules (e.g., non-empty names, restaurant ownership).

---



#### Phase B: Projector Coverage Extension (Days 4-5)

**B1: Extend Projector to Handle New Events**
- The existing `FullMenuView` projector relies on a naive rebuild strategy. We will extend this to trigger on `Menu` and `MenuCategory` events.

**Event Handlers to Implement:**
- `MenuCreatedEventHandler`
- `MenuDetailsUpdatedEventHandler`
- `MenuAvailabilityChangedEventHandler`
- `MenuCategoryAddedEventHandler`
- `MenuCategoryDetailsUpdatedEventHandler`
- `MenuCategoryRemovedEventHandler`

**Implementation:**
- All handlers will use the existing `IMenuReadModelRebuilder` service to rebuild the restaurant's `FullMenuView`.
- This ensures data consistency with minimal implementation effort, leveraging the reliable patterns from Step 2.

---

#### Phase C: Management Endpoints (Days 6-7)

**C1: Menu and Category API Endpoints**
- Create a new endpoint group `RestaurantMenuManagement` to house the new operations.

**Endpoint Structure:**
```csharp
// src/Web/Endpoints/RestaurantMenuManagement.cs
public class RestaurantMenuManagement : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization()
            .WithTags("Restaurant Menu Management");

        // Menu Endpoints
        group.MapPost("/{restaurantId:guid}/menus", CreateMenu);
        group.MapPut("/{restaurantId:guid}/menus/{menuId:guid}", UpdateMenuDetails);
        group.MapPut("/{restaurantId:guid}/menus/{menuId:guid}/availability", ChangeMenuAvailability);

        // Category Endpoints
        group.MapPost("/{restaurantId:guid}/menus/{menuId:guid}/categories", AddMenuCategory);
        group.MapPut("/{restaurantId:guid}/categories/{categoryId:guid}", UpdateMenuCategoryDetails);
        group.MapDelete("/{restaurantId:guid}/categories/{categoryId:guid}", RemoveMenuCategory);
    }
}
```

**Implementation:**
- Follow the existing pattern of dispatching commands via `ISender` and returning results using `.ToIResult()`.

---

#### Phase D: Testing Strategy (Days 8-9)

**D1: Unit & Functional Tests**
- **Domain:** Add tests for new business logic in `Menu` and `MenuCategory` aggregates.
- **Application:** Create tests for all new command handlers, validating logic, authorization, and error handling.
- **Web:** Add functional tests for the new management endpoints to ensure correct behavior and authorization.

**D2: Integration & End-to-End Tests**
- **Integration:** Verify that repository interactions and event publications work as expected.
- **End-to-End:** Create tests that simulate a full flow: API call -> Command -> Domain Event -> Projector -> `FullMenuView` update. This validates the entire feature from write to read-side synchronization.

---

### File Structure and Deliverables

**Application Layer:**
```
src/Application/
├── Menus/
│   ├── Commands/
│   │   ├── CreateMenu/
│   │   ├── UpdateMenuDetails/
│   │   └── ChangeMenuAvailability/
│   └── EventHandlers/
│       ├── MenuCreatedEventHandler.cs
│       └── ...
└── MenuCategories/
    ├── Commands/
    │   ├── AddMenuCategory/
    │   ├── UpdateMenuCategoryDetails/
    │   └── RemoveMenuCategory/
    └── EventHandlers/
        ├── MenuCategoryAddedEventHandler.cs
        └── ...
```

**Web Layer:**
```
src/Web/Endpoints/
└── RestaurantMenuManagement.cs
```

### Success Criteria

- Restaurant owners can create and manage menus.
- Restaurant staff can add, update, and remove categories within a menu.
- All changes to menus and categories are reflected in the public `FullMenuView` in near real-time.
- All new operations are protected by appropriate authorization policies.
- The system remains consistent and reliable under concurrent operations.
