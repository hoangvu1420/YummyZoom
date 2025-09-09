# Phase 2: Unified Implementation Outline (Menu Management + Public Browse)

## Executive Summary
Phase 2 delivers a complete menu management experience for restaurant owners/staff and a fast, public menu browsing experience for customers. It adheres to Clean Architecture with DDD and CQRS, using EF Core repositories for commands, Dapper for queries, and event-driven read models (FullMenuView) for high performance.

## What Already Exists to Leverage
- Denormalized public menu store:
  - Model: "src\Infrastructure\Data\Models\FullMenuView.cs"
  - EF config: "src\Infrastructure\Data\Configurations\FullMenuViewConfiguration.cs"
  - DbContext registration: "src\Infrastructure\Data\ApplicationDbContext.cs"
- Domain aggregates and events:
  - Menus/Categories: "src\Domain\MenuEntity\"
  - Menu Items: "src\Domain\MenuItemAggregate\"
- Dapper query stack and patterns already in use
- Outbox/Inbox patterns wired in Infrastructure for reliable, idempotent event handling
- Existing endpoint framework under: "src\Web\Endpoints\"
- Existing repository example: "src\Infrastructure\Data\Repositories\MenuItemRepository.cs"

## Unified Implementation Plan (Efficient Sequence)

### 1) Public Read Side First
- Implement `GetFullMenuQuery` reading `FullMenuViews` via Dapper; return `MenuJson` + `LastRebuiltAt`.
- Add `GET /api/public/restaurants/{restaurantId}/menu` emitting ETag/Last-Modified based on `LastRebuiltAt`.
- Add `GetRestaurantPublicInfoQuery` (minimal card) and a stubbed `SearchRestaurantsQuery` (text/cuisine/geo filters, pagination helpers reused).
- Temporary admin command to manually rebuild a restaurant’s FullMenuView for early end-to-end validation.

### 2) Event-Driven Projector for FullMenuView (naive, correct-first)
- Strategy: rebuild the restaurant’s FullMenuView JSON on any relevant change; structure code to allow future partial JSON patches.
- Event handlers to add (by aggregate):
  - Menu: Created, Enabled, Disabled, Removed
  - Menu Category: Added, NameUpdated, DisplayOrderUpdated, Removed
  - Menu Item: Created, Deleted, PriceChanged, AvailabilityChanged, DietaryTagsUpdated, CustomizationAssigned, CustomizationRemoved, AssignedToCategory
  - Customization Group/Tag changes affecting item presentation
- Implementation: idempotent handlers (Inbox); load authoritative data (EF/Dapper), compose DTO, serialize to JSON, upsert into `FullMenuViews`, set `LastRebuiltAt`; rely on Outbox publisher pipeline.

### 3) Application Layer – Commands (write side)
- Repositories/plumbing: add `IMenuRepository`, `IMenuCategoryRepository`; implement EF Core repos; register in DI; use UnitOfWork/transaction helpers in `ApplicationDbContext` to persist aggregates and outbox in the same commit.
- Menu Item commands (highest value first): `CreateMenuItem`, `UpdateMenuItemDetails`, `ChangeMenuItemAvailability`, `UpdateMenuItemDietaryTags`, `AssignCustomizationGroupToMenuItem`, `RemoveCustomizationGroupFromMenuItem`, `AssignMenuItemToCategory`, `DeleteMenuItem`.
- Menu Category commands: `AddCategory`, `RenameCategory`, `UpdateCategoryDisplayOrder`, `RemoveCategory`.
- Menu commands: `CreateMenu`, `EnableMenu`, `DisableMenu`, `UpdateMenuInfo`, `RemoveMenu`.
- Cross-cutting: FluentValidation for payloads; enforce `[Authorize(Policy = Policies.MustBeRestaurantStaff)]`; validate tenancy via role assignments and restaurant ownership.

### 4) Management Queries (Restaurant Operations)
- `GetMenusForManagementQuery`, `GetMenuCategoryDetailsQuery`, `GetMenuItemsByCategory`, `GetMenuItemDetailsQuery`.
- Hierarchical DTOs for nested structures; filtering/pagination; authorization scoping to restaurant context.

### 5) Web/API Endpoints
- Management (authenticated):
  - `POST /api/restaurants/{restaurantId}/menu`
  - `PUT /api/restaurants/{restaurantId}/menu`
  - `POST|PUT|DELETE /api/restaurants/{restaurantId}/menu/categories/...`
  - `POST|PUT|DELETE /api/restaurants/{restaurantId}/menu/items/...`
  - `PATCH` for price/availability/category assignment/customizations
- Public (anonymous):
  - `GET /api/public/restaurants/{restaurantId}/menu`
  - `GET /api/public/restaurants/search`
  - `GET /api/public/restaurants/{restaurantId}/info`
- Guidance: public endpoints use Dapper and are read-optimized; management uses CQRS command dispatch; emit ETag/Last-Modified based on `FullMenuView.LastRebuiltAt`.

### 6) Security, Tenancy, and Validation
- Role-based access for management; resource ownership validation; restaurant ID scoping for multi-tenant safety; audit key mutations.
- FluentValidation for payload constraints (name not empty, price ≥ 0, display order non-negative).
- Enforce domain invariants in aggregates using Result pattern and domain errors.

### 7) Caching and Performance
- HTTP caching via ETag/Last-Modified; appropriate Cache-Control for public endpoints.
- Return pre-baked JSON; avoid runtime joins; add read-side indexes as needed; reuse Dapper pagination helpers.

### 8) Backfill and Maintenance Jobs
- One-time backfill to build FullMenuView for all restaurants on deploy.
- Periodic reconciliation job to detect missing/aged views and rebuild (soft-fail safeguard).

### 9) Testing Strategy and Coverage
- Unit: domain logic, invariants, validators.
- Functional: command/query handlers, authorization policies.
- Integration: repositories and database interactions.
- End-to-end: command → domain event → outbox → projector → FullMenuView → public GET.
- Concurrency/idempotency: Inbox processing and transaction boundaries.

### 10) Deliverables Checklist
- Commands/Validators/Handlers for Menu, Categories, Items.
- Missing repositories implemented and wired in DI.
- FullMenuView projector handlers implemented and idempotent.
- Public queries: `GetFullMenu`, `SearchRestaurants`, `GetRestaurantPublicInfo`.
- Web endpoints for management and public APIs with authorization.
- Backfill and reconciliation jobs.
- Tests across unit/functional/integration/E2E.

### 11) Suggested Implementation Order (optimize feedback loop)
1. Public read side first: `GetFullMenuQuery` + GET endpoint; temporary manual rebuild command.
2. Implement key write flows for items; projector handles item events for one restaurant.
3. Expand to categories and menu-level commands; extend projector coverage.
4. Add public search and restaurant info endpoints (stub provider).
5. Add backfill job and complete the test matrix.
6. Harden with caching headers and optimize JSON structure if needed.

### 12) Data Contract for `FullMenuView.MenuJson` (recommended shape)

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

Notes
- Normalized maps (`byId`) with explicit `order` arrays avoid duplication and enable efficient diff/patch.
- `dietaryTagIds` references are resolved via `tagLegend` to avoid repeating tag names across many items.
- `currency` is the restaurant/menu default; prices also carry currency to allow future multi-currency scenarios.
- Exclude soft-deleted entities and disabled menus; include only currently visible categories/items.

## Technical Architecture Decisions

### CQRS Implementation
- Commands: EF Core repositories + UnitOfWork for consistency.
- Queries: Dapper + direct SQL for optimal performance.
- Read Models: event-driven projections for denormalized views.

### Event-Driven Architecture
- Domain events for all state changes.
- Asynchronous projector updates via Outbox; idempotent event handlers via Inbox.

### Performance Optimization
- FullMenuView for customer-facing queries (target sub-100ms).
- Efficient indexing strategy for search operations.
- Batch operations for administrative tasks.

### Security & Authorization
- Policy-based authorization with restaurant context.
- Resource ownership validation.
- Audit trail for management operations.

## Success Criteria
1. Restaurant owners efficiently manage complete menu structures.
2. Customers experience sub-100ms menu loading times.
3. Real-time availability updates propagate immediately.
4. Search functionality supports restaurant discovery.
5. Authorization prevents unauthorized menu modifications.
6. System maintains data consistency under concurrency.
7. Read models stay synchronized with write operations.

## Dependencies & Prerequisites
- Phase 1 order management system (✅ Complete)
- Domain layer menu entities (✅ Complete)
- Infrastructure EF configurations (✅ Complete)
- Authorization framework (✅ Complete)
- Outbox/Inbox event system (✅ Complete)
