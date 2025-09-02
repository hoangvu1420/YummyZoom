# Phase 2, Step 2: Implementation Outline
## Key Write Flows for Menu Items + Event-Driven Projector

### Executive Summary

This document provides a comprehensive implementation plan for Phase 2, Step 2: "Implement key write flows for items; projector handles item events for one restaurant." This step builds upon the completed Step 1 (public read side) to deliver core menu management capabilities for restaurant staff with real-time synchronization to the public menu view.

### Current State Analysis

**✅ Completed in Step 1:**
- Public read infrastructure: `GetFullMenuQuery`, public endpoints with caching
- `FullMenuViewRebuilder` service for manual rebuilds
- Manual admin rebuild command for testing
- Domain models with events: `Menu`, `MenuCategory`, `MenuItem`
- Authorization framework with restaurant-scoped policies
- Outbox/Inbox event processing infrastructure

**🔍 Available Infrastructure:**
- `IMenuItemRepository` interface and EF Core implementation
- Event handling patterns with `IdempotentNotificationHandler`
- Authorization policies: `MustBeRestaurantStaff`, `MustBeRestaurantOwner`
- Result pattern for error handling
- FluentValidation framework
- UnitOfWork pattern for transactions

**❌ Missing for Step 2:**
- `IMenuRepository` and `IMenuCategoryRepository` interfaces/implementations
- Write commands for menu item management
- Event handlers (projectors) for automatic FullMenuView updates
- Management endpoints for restaurant staff

---

## Implementation Plan

### Phase A: Foundation Infrastructure (Days 1-2)

#### A1: Repository Interfaces and Implementations

**Create Missing Repository Interfaces:**

```csharp
// src/Application/Common/Interfaces/IRepositories/IMenuRepository.cs
public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(MenuId menuId, CancellationToken cancellationToken = default);
    Task<Menu?> GetEnabledByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<List<Menu>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task AddAsync(Menu menu, CancellationToken cancellationToken = default);
    void Update(Menu menu);
}

// src/Application/Common/Interfaces/IRepositories/IMenuCategoryRepository.cs
public interface IMenuCategoryRepository
{
    Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);
    Task<List<MenuCategory>> GetByMenuIdAsync(MenuId menuId, CancellationToken cancellationToken = default);
    Task<List<MenuCategory>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task AddAsync(MenuCategory category, CancellationToken cancellationToken = default);
    void Update(MenuCategory category);
}
```

**Implement EF Core Repositories:**
- `src/Infrastructure/Data/Repositories/MenuRepository.cs`
- `src/Infrastructure/Data/Repositories/MenuCategoryRepository.cs`
- Follow existing patterns from `MenuItemRepository`

**Register in DI:**
- Add to `Infrastructure/DependencyInjection.cs`

#### A2: Domain Model Enhancements

**Verify/Add Missing Domain Methods:**
- `MenuItem.UpdateDetails(name, description, basePrice, imageUrl)`
- `MenuItem.UpdateDietaryTags(tagIds)`
- `MenuItem.AssignToCategory(categoryId)`
- Ensure all methods raise appropriate domain events

**Verify Required Events Exist:**
- `MenuItemCreated` ✅ (exists)
- `MenuItemAvailabilityChanged` ✅ (exists)
- `MenuItemAssignedToCategory` ✅ (exists)
- `MenuItemDetailsUpdated` (verify/create)
- `MenuItemDietaryTagsUpdated` (verify/create)
- `MenuItemDeleted` (verify/create)

---

### Phase B: Core MenuItem Commands (Days 3-5)

#### B1: High-Priority Commands

**1. CreateMenuItemCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid MenuCategoryId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string? ImageUrl = null,
    bool IsAvailable = true,
    List<Guid>? DietaryTagIds = null
) : IRequest<Result<CreateMenuItemResponse>>, IRestaurantCommand;
```

**2. ChangeMenuItemAvailabilityCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record ChangeMenuItemAvailabilityCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    bool IsAvailable
) : IRequest<Result>, IRestaurantCommand;
```

**3. UpdateMenuItemDetailsCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record UpdateMenuItemDetailsCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string? ImageUrl = null
) : IRequest<Result>, IRestaurantCommand;
```

**4. AssignMenuItemToCategoryCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record AssignMenuItemToCategoryCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    Guid NewCategoryId
) : IRequest<Result>, IRestaurantCommand;
```

**5. UpdateMenuItemDietaryTagsCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record UpdateMenuItemDietaryTagsCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    List<Guid> DietaryTagIds
) : IRequest<Result>, IRestaurantCommand;
```

**6. DeleteMenuItemCommand**
```csharp
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record DeleteMenuItemCommand(
    Guid RestaurantId,
    Guid MenuItemId
) : IRequest<Result>, IRestaurantCommand;
```

#### B2: Command Handlers Implementation

**Common Handler Pattern:**
- Validate input using FluentValidation
- Load aggregate using repository
- Validate business rules (category exists, restaurant ownership)
- Execute domain operation
- Save changes using UnitOfWork
- Return Result with appropriate errors

**Key Validation Rules:**
- Restaurant ownership verification
- MenuCategory exists and belongs to restaurant
- MenuItem exists and belongs to restaurant
- Business rule validation (name uniqueness within category, positive prices)

#### B3: FluentValidation Validators

**Create validators for each command:**
- Required field validation
- Business rule validation (positive prices, valid GUIDs)
- String length limits
- Currency code validation

---

### Phase C: Event-Driven Projector (Days 6-7)

#### C1: FullMenuView Projector Event Handlers

**Strategy: Naive Rebuild Approach**
- Rebuild entire restaurant's FullMenuView on any MenuItem event
- Simple, reliable, and performant for current scale
- Structure code to allow future partial update optimization

**Event Handlers to Implement:**

```csharp
// src/Application/MenuItems/EventHandlers/MenuItemCreatedEventHandler.cs
public sealed class MenuItemCreatedEventHandler : IdempotentNotificationHandler<MenuItemCreated>
{
    private readonly IMenuReadModelRebuilder _rebuilder;
    
    protected override async Task HandleCore(MenuItemCreated notification, CancellationToken ct)
    {
        await RebuildRestaurantMenu(notification.RestaurantId, ct);
    }
}
```

**Similar handlers for:**
- `MenuItemAvailabilityChangedEventHandler`
- `MenuItemDetailsUpdatedEventHandler`
- `MenuItemDietaryTagsUpdatedEventHandler`
- `MenuItemAssignedToCategoryEventHandler`
- `MenuItemDeletedEventHandler`

#### C2: Projector Implementation Details

**Shared Rebuild Logic:**
- Extract common rebuild logic to base class or shared service
- Use existing `IMenuReadModelRebuilder` service
- Handle errors gracefully with proper logging
- Ensure idempotency through Inbox pattern

**Error Handling:**
- Log rebuild failures but don't fail the original command
- Implement retry logic through Outbox mechanism
- Monitor rebuild performance and success rates

---

### Phase D: Management Endpoints (Days 8-9)

#### D1: Restaurant Management API Endpoints

**Endpoint Structure:**
```csharp
// src/Web/Endpoints/RestaurantMenuItems.cs
public class RestaurantMenuItems : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this)
            .RequireAuthorization()
            .WithTags("Restaurant Menu Management");

        // POST /api/restaurants/{restaurantId}/menu-items
        group.MapPost("/{restaurantId:guid}/menu-items", CreateMenuItem);
        
        // PUT /api/restaurants/{restaurantId}/menu-items/{itemId}/availability
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/availability", UpdateAvailability);
        
        // PUT /api/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}", UpdateMenuItem);
        
        // PUT /api/restaurants/{restaurantId}/menu-items/{itemId}/category
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/category", AssignToCategory);
        
        // PUT /api/restaurants/{restaurantId}/menu-items/{itemId}/dietary-tags
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/dietary-tags", UpdateDietaryTags);
        
        // DELETE /api/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapDelete("/{restaurantId:guid}/menu-items/{itemId:guid}", DeleteMenuItem);
    }
}
```

#### D2: Endpoint Implementation

**Common Patterns:**
- Use ISender to dispatch commands
- Return standardized responses using `.ToIResult()`
- Apply proper HTTP status codes
- Include validation error details in responses

---

### Phase E: Testing Strategy (Days 10-11)

#### E1: Unit Tests
- Domain model behavior and invariants
- Command handler logic and validation
- Event handler idempotency and error handling
- Repository implementations

#### E2: Functional Tests
- End-to-end command execution
- Authorization policy enforcement
- Event processing and projector updates
- API endpoint behavior

#### E3: Integration Tests
- Database interactions and transactions
- Event publishing and handling
- FullMenuView rebuild accuracy
- Concurrent operation handling

---

## File Structure and Deliverables

### Application Layer Files
```
src/Application/MenuItems/
├── Commands/
│   ├── CreateMenuItem/
│   │   ├── CreateMenuItemCommand.cs
│   │   ├── CreateMenuItemCommandHandler.cs
│   │   └── CreateMenuItemCommandValidator.cs
│   ├── ChangeMenuItemAvailability/
│   ├── UpdateMenuItemDetails/
│   ├── AssignMenuItemToCategory/
│   ├── UpdateMenuItemDietaryTags/
│   └── DeleteMenuItem/
└── EventHandlers/
    ├── MenuItemCreatedEventHandler.cs
    ├── MenuItemAvailabilityChangedEventHandler.cs
    ├── MenuItemDetailsUpdatedEventHandler.cs
    ├── MenuItemDietaryTagsUpdatedEventHandler.cs
    ├── MenuItemAssignedToCategoryEventHandler.cs
    └── MenuItemDeletedEventHandler.cs
```

### Infrastructure Layer Files
```
src/Infrastructure/Data/Repositories/
├── MenuRepository.cs
└── MenuCategoryRepository.cs
```

### Web Layer Files
```
src/Web/Endpoints/
└── RestaurantMenuItems.cs
```

### Test Files
```
tests/Application.UnitTests/MenuItems/
tests/Application.FunctionalTests/MenuItems/
tests/Infrastructure.IntegrationTests/Repositories/
```

---

## Technical Decisions and Considerations

### Architecture Decisions

**1. Event Handler Strategy**
- **Decision**: Naive rebuild approach for FullMenuView projector
- **Rationale**: Simpler implementation, reliable consistency, adequate performance
- **Future**: Structure allows optimization to partial updates later

**2. Authorization Approach**
- **Decision**: Use existing `MustBeRestaurantStaff` policy
- **Rationale**: Leverages existing infrastructure, restaurant owners inherit staff permissions
- **Implementation**: All commands implement `IRestaurantCommand`

**3. Repository Design**
- **Decision**: Follow existing patterns from `IMenuItemRepository`
- **Rationale**: Consistency with codebase, proven patterns
- **Scope**: Add only methods needed for Step 2 commands

### Performance Considerations

**1. Projector Performance**
- Rebuild entire restaurant menu on any item change
- Monitor rebuild times and optimize if needed
- Consider batching multiple events for same restaurant

**2. Database Queries**
- Use existing Dapper patterns for read operations
- Leverage EF Core change tracking for write operations
- Add indexes if query performance becomes an issue

### Error Handling Strategy

**1. Command Failures**
- Use Result pattern consistently
- Provide meaningful error messages
- Log errors for debugging

**2. Event Processing Failures**
- Don't fail original command if projector fails
- Use Outbox retry mechanism
- Monitor and alert on persistent failures

### Security Considerations

**1. Authorization**
- All commands require restaurant staff permissions
- Resource-based authorization prevents cross-restaurant access
- Input validation prevents injection attacks

**2. Data Validation**
- FluentValidation for input sanitization
- Domain invariants for business rule enforcement
- SQL parameterization prevents injection

---

## Success Criteria

### Functional Requirements
- ✅ Restaurant staff can create menu items through API
- ✅ Restaurant staff can update item availability in real-time
- ✅ Restaurant staff can modify item details (name, price, description)
- ✅ Restaurant staff can organize items by category
- ✅ Restaurant staff can manage dietary tags
- ✅ Restaurant staff can delete menu items
- ✅ Public menu view updates automatically when items change

### Non-Functional Requirements
- ✅ All operations properly authorized for restaurant context
- ✅ Commands validate input and business rules
- ✅ Event processing is idempotent and reliable
- ✅ System maintains consistency under concurrent operations
- ✅ Projector rebuilds complete within acceptable time limits
- ✅ Comprehensive test coverage across all layers

### Technical Requirements
- ✅ Follows Clean Architecture and DDD patterns
- ✅ Uses existing infrastructure and patterns
- ✅ Maintains backward compatibility
- ✅ Provides clear error messages and logging
- ✅ Supports future optimization and enhancement

---

## Risk Assessment and Mitigation

### High-Risk Areas

**1. Event Processing Reliability**
- **Risk**: Events lost or processed multiple times
- **Mitigation**: Use existing Outbox/Inbox patterns, comprehensive testing

**2. Projector Performance**
- **Risk**: Rebuild times become unacceptable
- **Mitigation**: Monitor performance, implement batching if needed

**3. Concurrent Operations**
- **Risk**: Race conditions in event processing
- **Mitigation**: Use database transactions, idempotent handlers

### Medium-Risk Areas

**1. Authorization Complexity**
- **Risk**: Incorrect permission checks
- **Mitigation**: Comprehensive functional tests, code review

**2. Data Consistency**
- **Risk**: FullMenuView out of sync with source data
- **Mitigation**: Reconciliation jobs, monitoring, manual rebuild capability

### Low-Risk Areas

**1. API Design**
- **Risk**: Poor endpoint design
- **Mitigation**: Follow existing patterns, API documentation

**2. Validation Logic**
- **Risk**: Insufficient input validation
- **Mitigation**: FluentValidation, domain invariants, testing

---

## Next Steps After Step 2

### Immediate Follow-ups
1. Implement menu and category management commands
2. Add public search and restaurant discovery endpoints
3. Implement backfill and reconciliation jobs
4. Add caching optimizations

### Future Enhancements
1. Optimize projector for partial updates
2. Add real-time notifications for menu changes
3. Implement bulk operations for menu management
4. Add analytics and reporting capabilities

---

## Conclusion

This implementation plan provides a comprehensive roadmap for delivering Phase 2, Step 2 functionality. The approach balances rapid delivery with architectural soundness, leveraging existing infrastructure while building incrementally toward the complete menu management system.

The plan prioritizes the highest-value operations for restaurant staff while establishing the event-driven foundation needed for real-time menu synchronization. The testing strategy ensures reliability and the risk mitigation approach addresses potential challenges proactively.

Upon completion, restaurant staff will have full menu item management capabilities with automatic synchronization to the public menu view, setting the foundation for the remaining Phase 2 features.
