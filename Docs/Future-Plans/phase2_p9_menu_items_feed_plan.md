# Phase 2 — Menu Items Feed (Home Page)

Status: Proposal
Owner: Web/API + Application teams
Last updated: 2025-10-11

## Summary

Frontend requests a curated, paginated feed of menu items to power the Home screen. While the Universal Search can return `menu_item` results, it lacks a dedicated popularity signal and the minimal fields the UI needs. This plan introduces a public endpoint that follows existing repository patterns (Clean Architecture, Dapper read models, EndpointGroupBase, contract + functional tests).

## Goals

- Provide a simple, fast API to list “popular” menu items with minimal fields.
- Reuse existing read models and conventions where possible; avoid heavy new infra.
- Keep the shape stable for infinite scroll (PaginatedList).

## API Specification (v1)

Endpoint: `GET /api/v1/menu-items/feed`

Query params:
- `tab` (string, required for now): `popular` (reserved: `new`, `nearby`, `recommended`)
- `pageNumber` (int, default 1, min 1)
- `pageSize` (int, default 20, range 1..50)

Response: `PaginatedList<MenuItemFeedDto>`

`MenuItemFeedDto` fields (minimal, FE-approved):
- `itemId` (UUID)
- `name` (string)
- `price` { `amount` (decimal), `currency` (string, ISO 4217) }
- `imageUrl` (string|null)
- `rating` (number|null) — restaurant average rating
- `restaurantName` (string)
- `restaurantId` (UUID)

Notes:
- Endpoint is public (no auth).
- Sorting depends on `tab`; for v1 we only implement `popular`.

## Data Sources (existing)

- `MenuItems` (id, name, BasePrice_Amount/Currency, ImageUrl, IsDeleted/IsAvailable, RestaurantId)
- `Restaurants` (id, name, IsDeleted)
- `RestaurantReviewSummaries` (restaurantId, AverageRating, TotalReviews)
- `Orders` + owned `OrderItems` (snapshot fields), for popularity signals

## Popularity Definition (v1)

Intent: Show items that are truly being ordered now. We will rank by recent order volume.

- Window: last 30 days
- Status: include fulfilled pipeline states — `Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`, `Delivered` (exclude `AwaitingPayment`, `Cancelled`, `Rejected`)
- Metric: `SUM(oi.Quantity)` grouped by `oi.Snapshot_MenuItemId`
- Tie-breakers: higher restaurant review count, higher average rating, most recently modified item

Fallback: If an item has no orders in the last 30 days, treat popularity as 0; it can still appear due to tie-breakers when a page needs filling.

## Query Design (Dapper; mirrors existing patterns)

- Create Application query: `GetMenuItemsFeedQuery(tab, pageNumber, pageSize)` returning `PaginatedList<MenuItemFeedDto>`.
- Implement handler with Dapper + `DapperPagination` (COUNT + page SQL), similar to `GetMenuItemsByCategoryQueryHandler` and search handlers.

SQL sketch (PostgreSQL):

```
WITH pop AS (
  SELECT oi."Snapshot_MenuItemId" AS "ItemId",
         CAST(SUM(oi."Quantity") AS int) AS qty30
  FROM "OrderItems" oi
  JOIN "Orders" o ON o."Id" = oi."OrderId"
  WHERE o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery','Delivered')
    AND o."PlacementTimestamp" >= now() - interval '30 days'
  GROUP BY oi."Snapshot_MenuItemId"
)
SELECT mi."Id"                AS "ItemId",
       mi."Name"              AS "Name",
       mi."BasePrice_Amount"  AS "PriceAmount",
       mi."BasePrice_Currency"AS "PriceCurrency",
       mi."ImageUrl"          AS "ImageUrl",
       r."Id"                 AS "RestaurantId",
       r."Name"               AS "RestaurantName",
       rr."AverageRating"     AS "Rating",
       COALESCE(pop.qty30, 0)  AS "Popularity"
FROM "MenuItems" mi
JOIN "Restaurants" r ON r."Id" = mi."RestaurantId"
LEFT JOIN "RestaurantReviewSummaries" rr ON rr."RestaurantId" = r."Id"
LEFT JOIN pop ON pop."ItemId" = mi."Id"
WHERE mi."IsDeleted" = FALSE AND r."IsDeleted" = FALSE AND mi."IsAvailable" = TRUE
```

Order by (tab = popular):

```
ORDER BY "Popularity" DESC NULLS LAST,
         COALESCE(rr."TotalReviews", 0) DESC,
         COALESCE(rr."AverageRating", 0) DESC,
         mi."LastModified" DESC,
         mi."Id" ASC
```

Paginate with `DapperPagination.BuildPagedSql`.

## Validation

- `tab` must be one of: `popular` (case-insensitive)
- `pageNumber` >= 1; `pageSize` in 1..50 (default 20)

## Web Endpoint

- New group: `MenuItems` → map under `/api/v1/menu-items` (consistent with `Restaurants`, `Search` groups)
- Route: `GET /feed`
- DTO binder for query params (Nullable ints for pagination to avoid early 400s; apply defaults inside)
- Returns `Results.Ok(PaginatedList<MenuItemFeedDto>)`

## OpenAPI / Docs

- Add docs under `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md` (or a new `03-Menu-Discovery.md` section) describing the feed, params, and schema. Mark `tab=popular` only for v1.

## Tests (follow existing structure)

Web.ApiContractTests (HTTP boundary):
- `tests/Web.ApiContractTests/MenuItems/FeedContractTests.cs`
  - 200 with default paging
  - 400 for invalid `tab`
  - Response shape matches: items[].{itemId,name,price{amount,currency},imageUrl,rating,restaurantName,restaurantId}

Application.FunctionalTests (behavior):
- `tests/Application.FunctionalTests/Features/MenuItems/Feed/MenuItemsFeedTests.cs`
  - Seed several menu items across restaurants
  - Create orders with different quantities to shape popularity
  - Assert ordering by popularity, tie-breakers by reviews, stable pagination

Testing notes:
- Reuse helpers for creating restaurants/menu items used elsewhere
- For reviews, use existing write paths or direct read model updates as done in other tests (pragmatic SQL updates where acceptable in tests)

## Performance and Indexing

Short term (aggregation on read):
- Ensure suitable indexes: `Orders(Status, PlacementTimestamp)`, `OrderItems(OrderId, Snapshot_MenuItemId)`, `MenuItems(IsDeleted, IsAvailable)`, `Restaurants(IsDeleted)`
- LIMIT/OFFSET with stable ordering and narrow projection

Longer term (phase 2):
- Add `MenuItemPopularity` read model updated by order lifecycle events (incremental counters per window: d7, d30, all‑time)
- Switch the feed to read from the popularity table for O(1) ranking

## Security

Public endpoint (no auth), same as search and restaurant discovery.

## Rollout Plan

1) App layer: query + validator + handler (Dapper)
2) Web: endpoint group + mapping + OpenAPI summary
3) Docs update (API reference)
4) Contract tests (HTTP) — pass; Functional tests — pass
5) Monitor: add basic logs/metrics: query latency, page hits

## Out of Scope / Future Tabs

- `new`: order by `mi.Created`/`LastModified` desc
- `nearby`: requires `lat/lon` + distance join to restaurant geo (reuse search logic)
- `recommended`: personalized signals

---

This plan follows the repository’s established patterns:
- Dapper read queries with `DapperPagination`
- EndpointGroupBase routing under `/api/v1`
- Public discovery endpoints in Customer API
- Web.ApiContractTests + Application.FunctionalTests separation

