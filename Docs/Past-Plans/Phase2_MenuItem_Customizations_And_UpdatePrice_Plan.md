# Phase 2 – Next Move: Menu Item Customizations + Dedicated Price Update

## Executive Summary
This plan delivers three management flows aligned with our Clean Architecture + DDD + CQRS approach:
- Assign a customization group to a menu item
- Remove a customization group from a menu item
- Update a menu item's price via a dedicated command (separate semantics from details update)

All commands enforce restaurant tenancy and staff authorization, emit domain events, and rely on existing event-driven projection to rebuild `FullMenuView`.

References to established patterns:
- Command style + validation + authorization: `src/Application/MenuItems/Commands/*`
- Repositories: `src/Infrastructure/Data/Repositories/*`
- Event-driven projector: `src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs`
- Public menu endpoint with ETag: `src/Web/Endpoints/Restaurants.cs`
- Outbox/Inbox idempotency: `src/Application/Common/Notifications/IdempotentNotificationHandler.cs`


## Scope & Goals
- Add application commands, validators, and handlers for the three flows
- Add minimal API endpoints under the authenticated Restaurant group
- Ensure domain events trigger projection (already wired)
- Add tests (unit + functional + API contracts) mirroring existing conventions

## Progress Checklist
- [x] Commands/Validators/Handlers: AssignCustomizationGroupToMenuItem
- [x] Commands/Validators/Handlers: RemoveCustomizationGroupFromMenuItem
- [x] Commands/Validators/Handlers: UpdateMenuItemPrice
- [ ] Web endpoints and DTOs for all three flows
- [ ] Unit tests for validators
- [ ] Functional tests (commands + outbox/inbox + projector assertions)
- [ ] API contract tests
- [ ] E2E manual validation and docs polish


## API Contracts (Management – Auth Required)
- POST `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations`
  - Body: `{ groupId: Guid, displayTitle: string, displayOrder?: int }`
  - Returns: 204 No Content on success; 404 NotFound if item or group missing; 400 on validation
  - Notes: if `displayOrder` omitted, handler assigns next order (1 + current max)

- DELETE `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations/{groupId}`
  - Returns: 204 No Content on success; 404 NotFound if item or assignment missing

- PUT `/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/price`
  - Body: `{ price: decimal, currency: string }`
  - Returns: 204 No Content on success; 404 NotFound if item missing; 400 on validation

Rationale:
- Follows existing endpoint style: PUT for targeted updates, DELETE for removals, POST for creating a new association. Naming mirrors current `category`, `dietary-tags`, `availability` routes in `src/Web/Endpoints/Restaurants.cs`.


## Application Layer Design

### 1) AssignCustomizationGroupToMenuItemCommand
- Record: `AssignCustomizationGroupToMenuItemCommand(Guid RestaurantId, Guid MenuItemId, Guid CustomizationGroupId, string DisplayTitle, int? DisplayOrder)`
- Interface/Attributes: `[Authorize(Policy = Policies.MustBeRestaurantStaff)]`, implements `IRestaurantCommand`
- Validator (FluentValidation):
  - `RestaurantId`, `MenuItemId`, `CustomizationGroupId` not empty
  - `DisplayTitle` required, max length 200 (consistent with name patterns), no leading/trailing whitespace
  - `DisplayOrder` optional, when provided must be `>= 0`
- Handler steps:
  - Load item by `MenuItemId`; 404 on missing
  - Enforce tenancy: `menuItem.RestaurantId.Value == RestaurantId` else `ForbiddenAccessException`
  - Load customization group by id (via `ICustomizationGroupRepository`)
    - If repository lacks `GetByIdAsync`, call `GetByIdsAsync(new[]{id})` and take single
    - 404 if not found or soft-deleted
  - Enforce group belongs to same restaurant
  - Compute `order = DisplayOrder ?? (menuItem.AppliedCustomizations.Any() ? max(displayOrder) + 1 : 1)`
  - Build `AppliedCustomization.Create(CustomizationGroupId.Create(id), DisplayTitle.Trim(), order)`
  - Call `menuItem.AssignCustomizationGroup(applied)`; propagate domain error (e.g., already assigned)
  - `_menuItemRepository.Update(menuItem)` in transactional `IUnitOfWork`
  - Success => Result.Success()

### 2) RemoveCustomizationGroupFromMenuItemCommand
- Record: `RemoveCustomizationGroupFromMenuItemCommand(Guid RestaurantId, Guid MenuItemId, Guid CustomizationGroupId)`
- Attributes/Interfaces: same as above
- Validator: all IDs required
- Handler steps:
  - Load item; 404 if missing
  - Enforce tenancy
  - Call `menuItem.RemoveCustomizationGroup(CustomizationGroupId.Create(id))`; if not found, return domain error mapped to 404/validation
  - `_menuItemRepository.Update(menuItem)` in transaction

### 3) UpdateMenuItemPriceCommand (Dedicated)
- Record: `UpdateMenuItemPriceCommand(Guid RestaurantId, Guid MenuItemId, decimal Price, string Currency)`
- Attributes/Interfaces: same as above
- Validator:
  - IDs required
  - `Price > 0` and `<= 9999.99m` (match existing detail validator bounds)
  - `Currency` length 3, uppercase `[A-Z]{3}`
- Handler steps:
  - Load item; 404 if missing
  - Enforce tenancy
  - Create `Money(Price, Currency)` and call `menuItem.UpdatePrice(money)`
  - `_menuItemRepository.Update(menuItem)` in transaction

Notes:
- Domain events already exist and are handled: `MenuItemCustomizationAssigned`, `MenuItemCustomizationRemoved`, `MenuItemPriceChanged`
- Event handlers already rebuild `FullMenuView` via `IMenuReadModelRebuilder`


## Web Endpoints
Edit `src/Web/Endpoints/Restaurants.cs` in Menu Management section:
- POST `/{restaurantId}/menu-items/{itemId}/customizations`
  - DTO: `AssignCustomizationRequestDto(Guid GroupId, string DisplayTitle, int? DisplayOrder)`
  - Map to `AssignCustomizationGroupToMenuItemCommand`
  - Return `result.ToIResult()`; document 204/400/404

- DELETE `/{restaurantId}/menu-items/{itemId}/customizations/{groupId}`
  - Map to `RemoveCustomizationGroupFromMenuItemCommand`
  - Return `result.ToIResult()`; document 204/404

- PUT `/{restaurantId}/menu-items/{itemId}/price`
  - DTO: `UpdatePriceRequestDto(decimal Price, string Currency)`
  - Map to `UpdateMenuItemPriceCommand`
  - Return `result.ToIResult()`; document 204/400/404

Ensure:
- `.RequireAuthorization()` is already applied to the management group
- `.WithName(...)`, `.WithSummary(...)`, `.WithDescription(...)`, and `.WithStandardResults()` consistent with nearby endpoints


## Repository/Infrastructure Considerations
- `ICustomizationGroupRepository` currently exposes `GetByIdsAsync`; use it for single fetch to avoid interface churn
- No DI changes needed; repository already registered in `Infrastructure.DependencyInjection`
- No changes to projector required; it already includes customization groups and options in the JSON document


## Validation & Authorization
- Use `[Authorize(Policy = Policies.MustBeRestaurantStaff)]` at command level
- Implement `IRestaurantCommand` to enforce tenancy checks and claims scoping
- Validators mirror style in `UpdateMenuItemDetailsCommandValidator`


## Testing Strategy
- Unit Tests (Validators)
  - Reject empty IDs, invalid currency, non-positive price, blank display title

- Functional Tests (Commands/Handlers)
  - Assign customization: creates domain event -> drain outbox -> `FullMenuView` updated (contains group under `items.byId[*].customizationGroups`)
  - Remove customization: event -> drain outbox -> customizationGroup removed from `FullMenuView`
  - Update price: event -> drain outbox -> item’s `price.amount/currency` changed in `FullMenuView`
  - Tenancy: attempts with mismatched `RestaurantId` throw `ForbiddenAccessException`
  - Idempotency via Inbox: repeat outbox draining yields no duplicate side-effects

- API Contract Tests
  - Route binding, status codes, request DTO shapes for the 3 endpoints
  - 204 on success; 404 NotFound and 400 ProblemDetails where applicable


## Rollout Steps
1. Add commands + validators + handlers under `src/Application/MenuItems/Commands/`:
   - `AssignCustomizationGroupToMenuItem/`
   - `RemoveCustomizationGroupFromMenuItem/`
   - `UpdateMenuItemPrice/`
2. Wire endpoints and DTOs in `src/Web/Endpoints/Restaurants.cs`
3. Add tests:
   - Validators (Application.UnitTests)
   - Functional (Application.FunctionalTests): command success + outbox/inbox + projector assertions
   - API Contract (Web.ApiContractTests): route/DTO/status
4. Verify local E2E happy path: create item -> assign customization -> update price -> public GET returns updated JSON with ETag changes
5. Documentation updates (this file + API notes if separate doc exists)


## Open Questions / Decisions
- Should `displayOrder` be optional and auto-assigned? Current plan: optional with auto-assign to `max+1` when omitted
- Error mapping: when removing a non-assigned group, map domain error to 404 vs 400; propose 404
- Consider a bulk assignment in the future (not in scope)


## Deliverables Checklist
- Commands/Validators/Handlers:
  - AssignCustomizationGroupToMenuItem
  - RemoveCustomizationGroupFromMenuItem
  - UpdateMenuItemPrice
- Web endpoints and DTOs for the 3 flows
- Tests: unit + functional + API contract
- Docs updated


## Alignment With Phase 2 Outline
- Management Commands: extends item commands with customization assignment and dedicated price update
- Event-Driven Projector: reuse naive rebuild; events already covered by existing handlers
- Web/API Endpoints: adds PATCH-style semantics but following current PUT/POST/DELETE patterns in code
- Security/Validation: enforce `Policies.MustBeRestaurantStaff` and FluentValidation
- Testing: functional and E2E coverage through outbox/inbox and `FullMenuView`
