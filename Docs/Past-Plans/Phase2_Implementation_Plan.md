
# Phase 2: Menu Management and Public Browse - Implementation Plan

Based on my analysis of the current project structure and the outlined requirements in Phase 2, here's a comprehensive plan to implement menu management and public browsing functionality:

## Executive Summary

Phase 2 focuses on implementing a complete menu management system for restaurant owners/staff and public menu browsing for customers. The implementation follows the established CQRS pattern with domain-driven design principles, leveraging the existing Menu, MenuCategory entities and MenuItem aggregate.

## Current State Analysis

✅ **Domain Layer**: Complete
- Menu, MenuCategory entities and MenuItem aggregate are fully implemented
- Domain events are properly defined and raised
- Business rules and invariants are enforced

✅ **Infrastructure Layer**: Partially Complete  
- EF Core configurations exist for all menu entities
- DbContext includes Menu, MenuCategory, and MenuItem
- FullMenuView read model structure exists but no projector
- MenuItemRepository exists but Menu/MenuCategory repositories missing

❌ **Application Layer**: Missing
- No menu management commands/queries implemented
- No repository interfaces for Menu/MenuCategory
- No event handlers for read model projections
- No authorization policies for restaurant staff

## Implementation Plan

### 1. Foundation Layer (Repository & Interfaces)
**Goal**: Establish data access patterns for Menu and MenuCategory entities

- **Create Repository Interfaces**: `IMenuRepository`, `IMenuCategoryRepository` in Application layer
- **Implement Repository Classes**: EF Core-based implementations in Infrastructure layer  
- **Register Dependencies**: Add to Infrastructure DI container
- **Update DbContext**: Ensure proper entity configurations

### 2. Menu Entity Management Commands
**Goal**: Enable restaurant owners/staff to manage their menu structure

**Commands to Implement**:
- `CreateMenuCommand` - Create new menu collections (e.g., "Lunch Menu")
- `UpdateMenuDetailsCommand` - Edit menu name and description
- `ToggleMenuStatusCommand` - Enable/disable entire menus
- `DeleteMenuCommand` - Remove menus with cascade handling

**Features**:
- Validation using FluentValidation  
- Authorization with `[Authorize(Policy = Policies.MustBeRestaurantStaff)]`
- Business rule enforcement (uniqueness checks)
- Proper error handling with Result pattern

### 3. MenuCategory Entity Management Commands  
**Goal**: Enable organization of menu items into logical sections

**Commands to Implement**:
- `CreateMenuCategoryCommand` - Create categories within menus
- `UpdateMenuCategoryDetailsCommand` - Edit category name and display order
- `ReorderMenuCategoriesCommand` - Batch reordering for drag-and-drop UI
- `DeleteMenuCategoryCommand` - Remove categories with orphan handling

**Features**:
- Batch operations for efficient UI interactions
- Display order management
- Cascading business logic for orphaned items

### 4. MenuItem Aggregate Management Commands
**Goal**: Complete CRUD operations for individual menu items

**Commands to Implement**:
- `CreateMenuItemCommand` - Add new items to categories
- `UpdateMenuItemCommand` - Edit item details, pricing, descriptions
- `ToggleMenuItemAvailabilityCommand` - Real-time inventory control
- `AssignCustomizationGroupCommand` - Link items to customization options
- `UpdateMenuItemCategoryCommand` - Move items between categories
- `DeleteMenuItemCommand` - Remove items from menu

**Features**:
- Price validation and currency handling
- Customization group associations
- Dietary tag management
- Image URL validation

### 5. Management Queries (Restaurant Operations)
**Goal**: Provide efficient data access for restaurant dashboards

**Queries to Implement**:
- `GetMenusForManagementQuery` - Hierarchical menu structure for admin UI
- `GetMenuCategoryDetailsQuery` - Category-specific management data
- `GetMenuItemsByCategory` - Filtered item listings
- `GetMenuItemDetailsQuery` - Individual item management data

**Features**:
- Optimized SQL with Dapper
- Hierarchical DTOs for nested menu structures
- Filtering and pagination support
- Authorization scoped to restaurant context

### 6. FullMenuView Projector System
**Goal**: Maintain denormalized, high-performance read model for customer apps

**Event Handlers to Implement**:
- `MenuCreatedEventHandler` - Rebuild menu view
- `MenuEnabledEventHandler` - Update menu availability
- `MenuCategoryAddedEventHandler` - Refresh category structure  
- `MenuItemCreatedEventHandler` - Add items to denormalized view
- `MenuItemAvailabilityChangedEventHandler` - Real-time availability updates

**Features**:
- JSON document storage for fast retrieval
- Incremental updates where possible
- Full rebuild fallback for complex changes
- Cache invalidation strategies

### 7. Public Menu Browsing Queries  
**Goal**: Lightning-fast menu browsing for customer-facing applications

**Queries to Implement**:
- `GetFullMenuQuery` - Complete restaurant menu for ordering
- `GetRestaurantMenuSummaryQuery` - High-level menu overview  
- `SearchMenuItemsQuery` - Text-based item search within restaurant
- `GetMenuItemDetailsQuery` - Individual item details with customizations

**Features**:
- Direct read from FullMenuView for maximum performance
- Support for filtering (dietary preferences, price range)
- Structured DTOs optimized for mobile/web clients
- Caching strategies for frequently accessed data

### 8. Restaurant Search Index 
**Goal**: Enable discovery of restaurants by cuisine, menu items, and location

**Components to Implement**:
- `RestaurantSearchIndex` read model
- `RestaurantIndexProjector` event handler
- `SearchRestaurantsQuery` with filtering capabilities
- Text search functionality (SQL LIKE initially, Elasticsearch later)

**Features**:
- Multi-field search (name, cuisine, popular items)
- Geospatial filtering capabilities
- Rating and review integration
- Performance-optimized for public APIs

### 9. Authorization & Security Layer
**Goal**: Ensure proper access control for menu management operations

**Policies to Implement**:
- `MustBeRestaurantStaff` - Owner or staff role verification
- Restaurant context validation for all commands/queries
- Menu ownership verification
- Resource-based authorization patterns

**Features**:
- Claims-based authentication integration
- Restaurant ID scoping for multi-tenant operations  
- Role hierarchy enforcement (Owner > Staff)
- Audit logging for management operations

### 10. Comprehensive Testing Suite
**Goal**: Ensure reliability and maintainability of menu management features

**Test Categories**:
- **Unit Tests**: Domain logic, validation rules, business invariants
- **Functional Tests**: Command/query handlers, authorization policies  
- **Integration Tests**: Repository implementations, database interactions
- **End-to-End Tests**: Complete user workflows via API

**Coverage Areas**:
- Happy path scenarios for all CRUD operations
- Error conditions and edge cases
- Authorization boundary testing
- Performance testing for read models
- Event handler idempotency verification

## Technical Architecture Decisions

### CQRS Implementation
- **Commands**: Use Repository pattern with EF Core for consistency
- **Queries**: Use Dapper with direct SQL for optimal performance
- **Read Models**: Event-driven projections for denormalized views

### Event-Driven Architecture  
- Domain events for all state changes
- Asynchronous projector updates via Outbox pattern
- Idempotent event handlers with Inbox pattern

### Performance Optimization
- FullMenuView for customer-facing queries (sub-100ms response times)
- Efficient indexing strategy for search operations
- Batch operations for administrative tasks

### Security & Authorization
- Policy-based authorization with restaurant context
- Resource ownership validation
- Audit trail for all management operations

## Success Criteria

1. **Restaurant owners can efficiently manage their complete menu structure**
2. **Customers experience sub-100ms menu loading times**  
3. **Real-time availability updates propagate immediately**
4. **Search functionality supports restaurant discovery**
5. **Authorization prevents unauthorized menu modifications**
6. **System maintains data consistency under concurrent operations**
7. **Read models stay synchronized with write operations**

## Dependencies & Prerequisites

- Phase 1 order management system (✅ Complete)
- Domain layer menu entities (✅ Complete)  
- Infrastructure EF configurations (✅ Complete)
- Authorization framework (✅ Complete)
- Outbox/Inbox event system (✅ Complete)

This plan provides a structured approach to implementing Phase 2 while maintaining the established architectural patterns and ensuring high performance for both management and customer-facing operations.





















---

What already exists to leverage
- Denormalized public menu store:
  - Model: <mcfile name="FullMenuView.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\Models\FullMenuView.cs"></mcfile>
  - EF config: <mcfile name="FullMenuViewConfiguration.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\Configurations\FullMenuViewConfiguration.cs"></mcfile>
  - DbContext registration: <mcfile name="ApplicationDbContext.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\ApplicationDbContext.cs"></mcfile>
- Domain aggregates and events:
  - Menus/Categories: <mcfolder name="MenuEntity" path="e:\source\repos\CA\YummyZoom\src\Domain\MenuEntity\"></mcfolder>
  - Menu Items: <mcfolder name="MenuItemAggregate" path="e:\source\repos\CA\YummyZoom\src\Domain\MenuItemAggregate\"></mcfolder>
- Dapper query stack and patterns already used broadly
- Outbox/Inbox patterns already wired in Infrastructure for reliable, idempotent event handling
- Existing endpoints framework to add APIs under: <mcfolder name="Endpoints" path="e:\source\repos\CA\YummyZoom\src\Web\Endpoints\"></mcfolder>
- Some repositories exist (e.g., menu item) to build on, e.g. <mcfile name="MenuItemRepository.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\Repositories\MenuItemRepository.cs"></mcfile>

Phase 2 high‑level plan

A) Domain readiness and gaps (light-touch)
- Verify domain APIs cover the following behaviors; add missing ones if needed (following Domain guidelines):
  - Menu: create, enable, disable, remove (soft), update core info
  - Menu Category: add, rename, change display order, remove
  - Menu Item: create, update core details (name/desc/price/image), change availability, update dietary tags, assign/unassign customization groups, assign to category, soft delete
- Ensure domain events already present fire for all above operations (they appear to exist from directory scan); add any missing events.
- Keep domain pure (no persistence concerns) and follow Result pattern for invariants and errors.

B) Application layer (Commands)
Create a Menu feature area under Application with Commands/Validators/Handlers (CQRS pattern).
- Menu commands (owner/staff role)
  - CreateMenu, EnableMenu, DisableMenu, UpdateMenuInfo, RemoveMenu
- Menu Category commands (scoped to a menu)
  - AddCategory, RenameCategory, UpdateCategoryDisplayOrder, RemoveCategory
- Menu Item commands (scoped to restaurant)
  - CreateMenuItem, UpdateMenuItemDetails, AssignMenuItemToCategory, ChangeMenuItemAvailability, UpdateMenuItemDietaryTags, AssignCustomizationGroupToMenuItem, RemoveCustomizationGroupFromMenuItem, DeleteMenuItem
- Implementation guidance
  - Each handler loads aggregates via repositories, calls domain methods, persists via UnitOfWork. Use existing transaction helpers in DbContext’s ExecuteInTransactionAsync to ensure domain events get converted to Outbox in the same commit: <mcfile name="ApplicationDbContext.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\ApplicationDbContext.cs"></mcfile>
  - Add missing repositories if required (IMenuRepository, IMenuCategoryRepository); follow existing repository patterns (e.g., MenuItemRepository).
  - Add FluentValidation validators per command; validate ownership/authorization context in handlers (see security below).

C) Application layer (Queries) for public browse
- GetFullMenuQuery
  - Inputs: RestaurantId
  - Output: the latest FullMenuView.MenuJson (and LastRebuiltAt for caching/ETag)
  - Implementation: Dapper over IDbConnectionFactory to read from FullMenuViews; no joins required because it’s denormalized.
- Restaurant public browse/search queries (initial/stub)
  - SearchRestaurantsQuery: basic text/cuisine/geo filters using the current DB (stub interface shaped for a future search index provider)
  - GetRestaurantPublicInfoQuery: returns verified/accepting-orders, name, logo, cuisine, delivery areas (minimum viable info for the public app)
- Pagination and performance
  - Reuse existing Dapper pagination helper for search results, where appropriate.
  - Keep FullMenuQuery lean and return a CDN-friendly JSON blob (not a decomposed object graph) for fast client hydration.

D) Event-driven projector for FullMenuView
- Strategy
  - Naive, correct-first: rebuild the entire restaurant’s FullMenuView on any menu/category/item/customization/tag change for that restaurant. This is simpler, reliable, and likely performant at current scale.
  - Optimization ready: structure projector to allow future partial JSON patches if needed.
- Event handlers to implement (grouped by aggregate type)
  - Menu: Created, Enabled, Disabled, Removed
  - Menu Category: Added, NameUpdated, DisplayOrderUpdated, Removed
  - Menu Item: Created, Deleted, PriceChanged, AvailabilityChanged, DietaryTagsUpdated, CustomizationAssigned, CustomizationRemoved, AssignedToCategory
  - Customization Group and Tag changes that affect menu item presentation
- Implementation
  - On event receipt, run idempotent handler (via Inbox) that rebuilds the restaurant’s menu document:
    - Load restaurant’s menus, categories, and items with necessary projections using EF Core or Dapper joins
    - Compute a structured DTO for the menu (categories + items + customizations)
    - Serialize to JSON and upsert into FullMenuViews (jsonb column), and set LastRebuiltAt
  - Follow Outbox/Inbox patterns to ensure reliable processing and natural backpressure via the existing OutboxPublisherHostedService pipeline
- Storage already set:
  - FullMenuView: <mcfile name="FullMenuView.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\Models\FullMenuView.cs"></mcfile>
  - JSON column and index configured: <mcfile name="FullMenuViewConfiguration.cs" path="e:\source\repos\CA\YummyZoom\src\Infrastructure\Data\Configurations\FullMenuViewConfiguration.cs"></mcfile>

E) Web/API endpoints
- Management endpoints (Authenticated: RestaurantOwner/Staff)
  - POST /api/restaurants/{restaurantId}/menu
  - PUT /api/restaurants/{restaurantId}/menu
  - POST/PUT/DELETE for categories: /api/restaurants/{restaurantId}/menu/categories/…
  - POST/PUT/DELETE for items: /api/restaurants/{restaurantId}/menu/items/…
  - PATCH endpoints for price/availability/category assignment/customizations
- Public browse endpoints (Anonymous)
  - GET /api/public/restaurants/{restaurantId}/menu → returns MenuJson + ETag
  - GET /api/public/restaurants/search → uses SearchRestaurantsQuery (stubbed)
  - GET /api/public/restaurants/{restaurantId}/info → minimal info card for listing/detail
- Implementation guidance
  - Follow existing minimal API endpoint patterns (same style as Orders/Restaurants under <mcfolder name="Endpoints" path="e:\source\repos\CA\YummyZoom\src\Web\Endpoints\"></mcfolder>)
  - Use Dapper for public queries (fast, read-optimized); use CQRS command dispatch for management commands
  - Emit ETag/Last-Modified based on FullMenuView.LastRebuiltAt for menu GETs

F) Security, tenancy, and validation
- Authorization
  - Enforce role-based access for management endpoints; only users with RoleAssignments for the target restaurant can mutate menu/category/item
- Tenancy safety
  - Validate restaurant ownership in command handlers by cross-checking RoleAssignment and target aggregate RestaurantId
- Validation and invariants
  - Apply FluentValidation for payload constraints (e.g., name not empty, price >= 0, display order non-negative)
  - Apply domain-level invariants in aggregates with meaningful domain errors

G) Caching and performance
- HTTP caching
  - Use ETag and/or Last-Modified based on FullMenuView.LastRebuiltAt; set Cache-Control for public endpoints
- CDN and payload
  - Return the pre-baked JSON; avoid heavy joins at request time
- Indexing
  - FullMenuViews.LastRebuiltAt index already present for maintenance queries; add any needed read-side indexes for search stubs

H) Backfill and maintenance jobs
- One-time backfill: build FullMenuView for all existing restaurants upon deploying this phase
- Reconciliation: a background job to detect missing/aged views and rebuild (soft-fail safe guard)

I) Testing plan (must-have coverage)
- Application functional tests for each command happy-path and key failure cases (auth, validation, invariants)
- Projector tests: for each relevant event, assert FullMenuView rebuilds correctly and idempotently
- Query tests: ensure Dapper queries return correct shapes and paginate properly
- Integration tests: end-to-end flow from command → domain event → outbox → projector → FullMenuView → public GET
- Concurrency/idempotency tests for event handling (Inbox store) and transaction boundaries

J) Deliverables checklist
- Application/Commands/Queries for Menu, Categories, Items (with validators)
- Repositories for missing aggregates (Menu, MenuCategory) and wiring in DI
- Event handlers to rebuild FullMenuView on all relevant domain events
- Public browse queries: GetFullMenu, SearchRestaurants, GetRestaurantPublicInfo
- Web endpoints for management and public APIs, with authorization wired
- Backfill job and optional periodic reconciliation job
- Tests as specified above

K) Suggested implementation order (optimize feedback loop)
1) Public read side first
   - Implement GetFullMenuQuery and the GET /api/public/restaurants/{id}/menu endpoint
   - Implement a temporary “manual rebuild” administrative command for FullMenuView (to test the shape end-to-end quickly)
2) Implement key write flows for items (highest user value)
   - Create/Update/Delete MenuItem; ChangeAvailability; UpdatePrice; AssignToCategory; UpdateDietaryTags
   - Projector: handle menu item events and ensure rebuild works for one restaurant
3) Expand to categories and menu-level commands
   - Add/Rename/Remove/DisplayOrder for categories; menu enable/disable
   - Extend projector to cover category/menu events
4) Add public search and restaurant info endpoints (stubbed provider)
5) Add backfill job and complete the test matrix
6) Harden with caching headers and optimize JSON structure if needed

L) Data contract for FullMenuView.MenuJson (recommended shape)
- Root structure for FullMenuView:

```json
    restaurantId: <UUID>,
    lastRebuiltAt: <DateTime>,
    categories: [
        {
            id: <UUID>,
            name: <string>,
            displayOrder: <int>,
            items: [
                {
                    id: <UUID>,
                    name: <string>,
                    description: <string>,
                    price: <decimal>,
                    currency: <string>,
                    isAvailable: <bool>,
                    dietaryTags: [<string>],
                    imageUrl: <string>,
                    customizationGroups: [
                        {
                            id: <UUID>,
                            name: <string>,
                            min: <int>,
                            max: <int>,
                            options: [
                                {
                                    id: <UUID>,
                                    name: <string>,
                                    priceDelta: <decimal>
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    ]
```

- Keep this in a dedicated DTO in Application layer to serialize consistently; store the serialized JSON in FullMenuViews jsonb column

M) Definition of Done (Phase 2)
- All management commands implemented with validators, authorization, and tests
- FullMenuView projector handles all relevant events and is idempotent
- Public endpoints for menu, search, and restaurant info live, cached, and tested
- One-time backfill executed successfully on existing data
- CI green: unit, functional, and integration tests pass

If you want, I can break this into a concrete, file-by-file todo plan across layers and start implementing the first slice (public FullMenuView GET plus a temporary rebuild command) to give you a fast end-to-end vertical slice.
        