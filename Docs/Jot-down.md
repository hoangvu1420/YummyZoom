## Overall Status of Implementation

Reference the outlined spec in Docs\Future-Plans\Phase2_Unified_Implementation_Outline.md . This document summarizes current implementation status and gaps.

Broadly on track: read side, public endpoints, event-driven rebuilds, core write flows in place.
Remaining gaps: some management queries, customization assignment commands/endpoints, menu removal handler, projector coverage for customization/tag aggregates, backfill/recon jobs.

## Public Read Side

GetFullMenu: implemented via Dapper with canonical row mapping.
src/Application/Restaurants/Queries/GetFullMenu/GetFullMenuQuery.cs:1
src/Application/Restaurants/Queries/GetFullMenu/GetFullMenuQueryHandler.cs:1
src/Application/Restaurants/Queries/Common/MenuDtos.cs:1
GET endpoint with ETag + Last-Modified + Cache-Control: implemented.
src/Web/Endpoints/Restaurants.cs:1 (public group, GET /{restaurantId}/menu)
src/Web/Infrastructure/Http/HttpCaching.cs:1
Restaurant public info and search: implemented (search currently stubbed for text/cuisine; geo TBD).
src/Application/Restaurants/Queries/GetRestaurantPublicInfo/*
src/Application/Restaurants/Queries/SearchRestaurants/*

## Event Projector (FullMenuView)

Naive, correct-first rebuild + upsert/delete implemented.
src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs:1
Event handlers cover many menu/menu-category/menu-item events with inbox idempotency:
Menus: created/updated/enabled/disabled.
src/Application/Menus/EventHandlers/*
Categories: added/name-updated/display-order-updated/removed.
src/Application/MenuCategories/EventHandlers/*
Items: created/deleted/price/availability/tags/customizations assigned/removed/assigned to category.
src/Application/MenuItems/EventHandlers/*
Missing projector coverage: CustomizationGroup and Tag aggregate change events (group/choice updates won’t rebuild yet).

## Write Side (Commands/Repositories)

EF Core repositories wired in DI: menus, categories, items (and others).
src/Infrastructure/DependencyInjection.cs:1
src/Infrastructure/Data/Repositories/MenuRepository.cs:1
src/Infrastructure/Data/Repositories/MenuCategoryRepository.cs:1
src/Infrastructure/Data/Repositories/MenuItemRepository.cs:1
Menu item commands: create, update details (incl. price), change availability, assign to category, update dietary tags, delete – all present with FluentValidation + authorization.
src/Application/MenuItems/Commands/*
Menu category commands: add, update (incl. display order), remove – present.
src/Application/MenuCategories/Commands/*
Menu commands: create, update details, change availability – present; remove menu command missing.
src/Application/Menus/Commands/*

## Web Endpoints

Management endpoints for menus/categories/items implemented on an authorized group.
src/Web/Endpoints/Restaurants.cs:1 (POST/PUT/DELETE for create/update/assign/remove flows)
Public endpoints: menu/info/search implemented and read-optimized (Dapper/ETag).
src/Web/Endpoints/Restaurants.cs:1

## Security & Validation

Policies, claims-based permissions, and validators are wired and used.
src/SharedKernel/Constants/Policies.cs:1
src/Infrastructure/DependencyInjection.cs:1 (policy registration)
Command/query validators across features (e.g., *Validator.cs)

## Read Model/DB Plumbing

FullMenuView model + EF config + DbContext registration present.
src/Infrastructure/Data/Models/FullMenuView.cs:1
src/Infrastructure/Data/Configurations/FullMenuViewConfiguration.cs:1
src/Infrastructure/Data/ApplicationDbContext.cs:1 (DbSet)
Dapper connection factory registered and used consistently.
src/Infrastructure/Data/DbConnectionFactory.cs:1
src/Infrastructure/Infrastructure.csproj:1

## Backfill & Maintenance Jobs

Manual admin command to rebuild a restaurant’s view: present (no web endpoint).
src/Application/Admin/Commands/RebuildFullMenu/*
No one-time backfill job or periodic reconciliation hosted service yet.

## Tests (Notable)

Public endpoints contracts: menu/info/search behavior and parameter binding.
tests/Web.ApiContractTests/Restaurants/*
GetFullMenu functional tests and row shape/offset checks.
tests/Application.FunctionalTests/Features/Restaurants/Queries/GetFullMenuQueryTests.cs:1
Event handling idempotency and projector effects (outbox→inbox→view).
tests/Application.FunctionalTests/Features/Menus/Events/*
tests/Application.FunctionalTests/OutboxInbox/OutboxFlowTests.cs:1
Admin rebuild command functional coverage.
tests/Application.FunctionalTests/Features/Admin/RebuildFullMenuCommandTests.cs:1
Domain unit tests for menu categories.
tests/Domain.UnitTests/MenuEntity/MenuCategoryTests.cs:1

## Gaps / TODOs

Customizations:
Missing commands/endpoints: assign/remove customization group on item (domain supports it).
Missing projector handlers for CustomizationGroup/Choice/Tag aggregate events (to rebuild affected menus).
Management queries: not implemented (e.g., GetMenusForManagement, GetMenuCategoryDetails, GetMenuItemsByCategory, GetMenuItemDetails).
Menu removal: no handler for MenuRemoved; current disabled handler deletes the view, but removal should also delete.
Backfill/reconciliation:
No one-time job to build all FullMenuView rows; no periodic recon job.
Admin trigger:
Manual rebuild command exists but no API endpoint to invoke it (if desired).
Search:
Geo filtering not yet implemented (currently text/cuisine stub as intended).

## Quick Next Steps

- [x] Implement missing commands + endpoints:
  Assign/Remove customization group to menu item.
  Optional: dedicated UpdatePrice if you want separate semantics.
- [x] Add projector handlers for:
  CustomizationGroupCreated/Deleted/ChoiceAdded/Removed/Updated, Tag changes.
- [ ] Implement management read-side queries (DTOs + Dapper) for staff UI.
- [ ] Add MenuRemovedEventHandler to delete FullMenuView.
- [ ] Add one-shot backfill and periodic reconciliation hosted service using IMenuReadModelRebuilder.
- [ ] Optionally expose an admin endpoint for RebuildFullMenu (protected by admin policy).
- [ ] Extend SearchRestaurantsQueryHandler with geospatial filters (PostGIS ST_DWithin) when ready.