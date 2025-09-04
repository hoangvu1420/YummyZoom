# Phase 2: Management Read-Side Queries (DTOs + Dapper) — Implementation Plan

## Overview
Implements staff-facing, management read queries using Dapper with explicit SQL, scoped by restaurant, and aligned with our CQRS approach. These queries provide fast, predictable data for menu management screens and are independent of the public `FullMenuView` projector.

## Current Context
- Public read-side is complete (`GetFullMenu`, caching headers, Dapper patterns).
- Write-side commands for menus, categories, items are implemented with EF + validators.
- Projector covers many events; CustomizationGroup/Tag events still pending.
- Management read-side queries are currently missing per Docs/Jot-down.md.

## Goals
- Deliver fast, deterministic, staff-scoped reads for menu operations.
- Use explicit SQL via Dapper; exclude soft-deleted rows; enforce multi-tenant safety.
- Provide minimal yet sufficient DTOs for staff UI lists and details.
- Keep queries independent of public denormalized views.

## Non-Goals (Now)
- Expanding to tag name resolution and customization group option catalogs in these handlers.
- HTTP caching for management endpoints (public endpoints already handle caching).

## Technical Decisions
- CQRS: EF for commands; Dapper for queries.
- Postgres SQL, parameterized queries; no implicit soft-delete filters—add `"IsDeleted" = false` explicitly.
- Deterministic ordering (e.g., `Name ASC, Id ASC`).
- Pagination via `DapperPagination.BuildPagedSql` + `QueryPageAsync`.
- Authorization: enforce restaurant scoping in WHERE; return NotFound if out-of-scope.

## Scope (Queries + DTOs)
1) GetMenusForManagement
   - Purpose: List menus for a restaurant with quick counts for categories/items.
   - Response: `IReadOnlyList<MenuSummaryDto>`
   - DTO: `MenuSummaryDto(MenuId, Name, Description, IsEnabled, LastModified, CategoryCount, ItemCount)`

2) GetMenuCategoryDetails
   - Purpose: Fetch category info and counts, with parent menu context (name/id).
   - Response: `MenuCategoryDetailsDto`
   - DTO: `MenuCategoryDetailsDto(MenuId, MenuName, CategoryId, Name, DisplayOrder, ItemCount, LastModified)`

3) GetMenuItemsByCategory (paged, with filters)
   - Purpose: List items within a category for the restaurant; optional `q`, `isAvailable`.
   - Response: `PaginatedList<MenuItemSummaryDto>`
   - DTO: `MenuItemSummaryDto(ItemId, Name, PriceAmount, PriceCurrency, IsAvailable, ImageUrl?, LastModified)`

4) GetMenuItemDetails
   - Purpose: Detailed item read for edit screen.
   - Response: `MenuItemDetailsDto`
   - DTO: `MenuItemDetailsDto(ItemId, CategoryId, Name, Description, PriceAmount, PriceCurrency, IsAvailable, ImageUrl?, DietaryTagIds: Guid[], AppliedCustomizations: { groupId, displayTitle, displayOrder }[], LastModified)`

Notes
- Keep DTOs lean for the UI; load tag names or customization group options via separate lookups when needed.
- Serialize JSON-backed columns as text and deserialize selectively in the handler (e.g., `DietaryTagIds`, `AppliedCustomizations`).

## SQL Sketches (Postgres)
- GetMenusForManagement
  ```sql
  SELECT
    m."Id"            AS MenuId,
    m."Name"          AS Name,
    m."Description"   AS Description,
    m."IsEnabled"     AS IsEnabled,
    m."LastModified"  AS LastModified,
    (SELECT COUNT(1) FROM "MenuCategories" mc WHERE mc."MenuId" = m."Id" AND mc."IsDeleted" = false) AS CategoryCount,
    (
      SELECT COUNT(1)
      FROM "MenuItems" mi
      JOIN "MenuCategories" mc ON mi."MenuCategoryId" = mc."Id"
      WHERE mc."MenuId" = m."Id" AND mi."IsDeleted" = false AND mc."IsDeleted" = false
    ) AS ItemCount
  FROM "Menus" m
  WHERE m."RestaurantId" = @RestaurantId AND m."IsDeleted" = false
  ORDER BY m."Name" ASC, m."Id" ASC;
  ```

- GetMenuCategoryDetails
  ```sql
  SELECT
    mc."MenuId"         AS MenuId,
    m."Name"            AS MenuName,
    mc."Id"             AS CategoryId,
    mc."Name"           AS Name,
    mc."DisplayOrder"   AS DisplayOrder,
    mc."LastModified"   AS LastModified,
    (SELECT COUNT(1) FROM "MenuItems" mi WHERE mi."MenuCategoryId" = mc."Id" AND mi."IsDeleted" = false) AS ItemCount
  FROM "MenuCategories" mc
  JOIN "Menus" m ON mc."MenuId" = m."Id"
  WHERE mc."Id" = @CategoryId AND m."RestaurantId" = @RestaurantId AND mc."IsDeleted" = false AND m."IsDeleted" = false;
  ```

- GetMenuItemsByCategory (paged)
  ```sql
  -- Build WHERE parts in code: RestaurantId, CategoryId, optional q (ILIKE), isAvailable
  SELECT
    mi."Id"             AS ItemId,
    mi."Name"           AS Name,
    mi."BasePrice_Amount"   AS PriceAmount,
    mi."BasePrice_Currency" AS PriceCurrency,
    mi."IsAvailable"    AS IsAvailable,
    mi."ImageUrl"       AS ImageUrl,
    mi."LastModified"   AS LastModified
  FROM "MenuItems" mi
  WHERE mi."RestaurantId" = @RestaurantId
    AND mi."MenuCategoryId" = @CategoryId
    AND mi."IsDeleted" = false
    /* AND (mi."Name" ILIKE '%' || @q || '%') */
    /* AND mi."IsAvailable" = @isAvailable */
  ORDER BY mi."Name" ASC, mi."Id" ASC
  LIMIT @Limit OFFSET @Offset;
  ```

- GetMenuItemDetails
  ```sql
  SELECT
    mi."Id"                 AS ItemId,
    mi."MenuCategoryId"     AS CategoryId,
    mi."Name"               AS Name,
    mi."Description"        AS Description,
    mi."BasePrice_Amount"   AS PriceAmount,
    mi."BasePrice_Currency" AS PriceCurrency,
    mi."IsAvailable"        AS IsAvailable,
    mi."ImageUrl"           AS ImageUrl,
    to_jsonb(mi."DietaryTagIds")::text        AS DietaryTagIdsJson,
    to_jsonb(mi."AppliedCustomizations")::text AS AppliedCustomizationsJson,
    mi."LastModified"       AS LastModified
  FROM "MenuItems" mi
  WHERE mi."Id" = @ItemId AND mi."RestaurantId" = @RestaurantId AND mi."IsDeleted" = false;
  ```

## Handlers and Structure
- Location: `src/Application/Restaurants/Queries/Management/...`
  - `GetMenusForManagement/{Query,Handler,Validator}.cs`
  - `GetMenuCategoryDetails/{Query,Handler,Validator}.cs`
  - `GetMenuItemsByCategory/{Query,Handler,Validator}.cs`
  - `GetMenuItemDetails/{Query,Handler,Validator}.cs`
- Dependencies: `IDbConnectionFactory`, `DapperPagination` (for paged query), `PaginatedList<T>`.
- Validation: guard `RestaurantId`, `CategoryId`, `ItemId`, `pageNumber>=1`, `pageSize>=1`.
- Errors: `NotFound` per feature, patterned after `GetFullMenuErrors`.

## Endpoints (to wire under staff group)
- `GET /api/v1/restaurants/{restaurantId}/menus` → list menus (MenuSummaryDto[])
- `GET /api/v1/restaurants/{restaurantId}/categories/{categoryId}` → one category (MenuCategoryDetailsDto)
- `GET /api/v1/restaurants/{restaurantId}/categories/{categoryId}/items` → paginated items (MenuItemSummaryDto)
  - Query params: `q?`, `isAvailable?`, `pageNumber`, `pageSize`
- `GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}` → item details (MenuItemDetailsDto)

Notes
- All routes require authorization and enforce scoping using `RestaurantId` in the query.
- 404 when entity does not belong to restaurant or is soft-deleted.

## Rollout Steps
For each management query (e.g., `GetMenusForManagement`, `GetMenuCategoryDetails`, `GetMenuItemsByCategory`, `GetMenuItemDetails`), implement the following steps as a complete vertical slice:

1) **Define Contracts & DTOs**:
   - Create the query record (e.g., `GetMenusForManagementQuery.cs`).
   - Define the response DTOs (e.g., `MenuSummaryDto.cs`, `MenuCategoryDetailsDto.cs`).
   - Establish specific error types for the query, patterned after `GetFullMenuErrors`.

2) **Implement Validator**:
   - Add the corresponding validator (e.g., `GetMenusForManagementQueryValidator.cs`) to validate IDs, paging parameters, and filter inputs.

3) **Implement Query Handler**:
   - Develop the Dapper query handler (e.g., `GetMenusForManagementQueryHandler.cs`) with explicit SQL, including soft-delete filters and enforcing `RestaurantId` for tenancy scoping.

4) **Wire Endpoint**:
   - Add the authorized GET endpoint to the `Restaurants.cs` staff group, ensuring correct route parameters and query string handling.

5) **Write Functional Tests**:
   - Create comprehensive functional tests for the specific query, covering:
     - Happy paths and successful data retrieval.
     - Tenancy scoping (verifying 404 for entities not belonging to the requested `RestaurantId`).
     - Pagination and deterministic ordering (where applicable).
     - Filter application (e.g., `q` and `isAvailable` for item lists).

6) **Document Query**:
   - Add brief usage notes, example responses, and parameter descriptions for the implemented query to relevant documentation.

## Testing Strategy
- Handlers: functional tests to DB using seed data; verify row-to-DTO mapping.
- Endpoints: contract tests for binding, 200/404/400 statuses, and payload shapes.
- Edge cases: empty results, large page sizes (bounded), soft-deleted rows, disabled menus.

## Risks & Follow-ups
- CustomizationGroup/Tag projector gaps do not block management reads (source-of-truth tables are queried), but UI fields relying on tag names/options may require auxiliary lookups later.
- Consider adding read-side indexes for common WHERE/ORDER combinations if profiling shows need.
- Potential future additions: `GetRestaurantTagLegend`, `GetCustomizationGroupDetails` to enrich management UI.

## Estimates
- Queries + handlers + validators: 1–2 days
- Endpoints + docs: 0.5 day
- Tests: 1 day

