# Restaurant Dashboard Endpoint Plan

**Date:** 2025-02-14
**Status:** Draft

## 1. Overview
Plan a restaurant-owner/staff dashboard endpoint that serves a single restaurant only. This is not the admin dashboard; it focuses on the current restaurant's operational and performance summary.

## 2. Current Backend Inventory

### 2.1 Existing endpoints already useful for dashboard data
- `GET /api/v1/restaurants/{restaurantId}/orders/new` (new orders queue)
- `GET /api/v1/restaurants/{restaurantId}/orders/active` (in-progress orders)
- `GET /api/v1/restaurants/{restaurantId}/orders/history` (completed/filtered orders)
- `GET /api/v1/restaurants/{restaurantId}/orders/{orderId}` (order details)
- `GET /api/v1/restaurants/{restaurantId}/reviews/summary` (public; uses `RestaurantReviewSummaries`)

### 2.2 Admin-only dashboard endpoints (not used for restaurant staff)
- `GET /admin/dashboard/summary` -> `AdminPlatformMetricsSnapshots`
- `GET /admin/dashboard/trends` -> `AdminDailyPerformanceSeries`
- `GET /admin/dashboard/restaurants` -> `AdminRestaurantHealthSummaries`

## 3. Database Tables From `db_scripts/schema.sql` (Relevant to restaurant dashboard)

### 3.1 Operational data
- `Orders` (status, totals, placement timestamps, restaurant id)
- `OrderItems` (item counts and line totals)
- `PaymentTransactions` (transaction amounts/statuses)
- `Restaurants` (name, logo, isVerified, isAcceptingOrders)

### 3.2 Financial data
- `RestaurantAccounts` (current balance)
- `AccountTransactions` (ledger of debits/credits)

### 3.3 Reviews and quality
- `Reviews` (raw review data)
- `RestaurantReviewSummaries` (read model, single row per restaurant)

### 3.4 Menu performance
- `MenuItems`, `MenuCategories`, `Menus` (catalog and counts)
- `MenuItemSalesSummaries` (read model for top-selling items)

### 3.5 Other tables that might inform dashboards later
- `Coupons`, `CouponUserUsages` (promotions impact)
- `FullMenuViews` (read model for current menu snapshot)

## 4. Read Models in the DB (including admin read models)
- `AdminPlatformMetricsSnapshots` (platform KPI snapshot)
- `AdminDailyPerformanceSeries` (platform daily series)
- `AdminRestaurantHealthSummaries` (per-restaurant health for admin)
- `RestaurantReviewSummaries` (per-restaurant review summary)
- `MenuItemSalesSummaries` (per-restaurant menu item counters)
- `FullMenuViews` (per-restaurant menu snapshot)
- `SearchIndexItems` (search projection)

Note: `ActiveCouponViews` exists as a read model in code but is not present in `schema.sql`. Confirm whether it is a view or a pending migration before depending on it.

## 5. Proposed Endpoint (Restaurant Dashboard, Per Restaurant)

### 5.1 Endpoint shape
`GET /api/v1/restaurants/{restaurantId}/dashboard/summary`

**Authorization**: `Policies.MustBeRestaurantStaff` (owner/staff only)

### 5.2 Response payload (draft)
```json
{
  "restaurant": {
    "id": "GUID",
    "name": "string",
    "logoUrl": "string",
    "isVerified": true,
    "isAcceptingOrders": true
  },
  "orders": {
    "newCount": 0,
    "activeCount": 0,
    "lastOrderAtUtc": "2025-02-14T00:00:00Z"
  },
  "sales": {
    "ordersLast7Days": 0,
    "ordersLast30Days": 0,
    "revenueLast7Days": 0.0,
    "revenueLast30Days": 0.0
  },
  "reviews": {
    "averageRating": 0.0,
    "totalReviews": 0,
    "lastReviewAtUtc": "2025-02-14T00:00:00Z"
  },
  "topItems": [
    {
      "menuItemId": "GUID",
      "name": "string",
      "rolling7DayQuantity": 0,
      "rolling30DayQuantity": 0
    }
  ],
  "balance": {
    "currentBalance": 0.0,
    "currency": "USD"
  },
  "lastUpdatedAtUtc": "2025-02-14T00:00:00Z"
}
```

### 5.3 Data sources per field
- `restaurant`: `Restaurants`
- `orders`: `Orders` (status + placement timestamps)
- `sales`: `Orders` + `PaymentTransactions` (or computed from `Orders.TotalAmount_*`)
- `reviews`: `RestaurantReviewSummaries`
- `topItems`: `MenuItemSalesSummaries` joined with `MenuItems`
- `balance`: `RestaurantAccounts` (+ optional recent ledger from `AccountTransactions`)

## 6. Implementation Notes (later work)
- Add a new query/handler in `src/Application/Restaurants/Queries/Management` (Dapper or EF).
- Add a new endpoint group file: `src/Web/Endpoints/Restaurants.Dashboard.cs`.
- Keep pagination for `topItems` optional (default top 5 or 10).
- Avoid pulling admin read models; they are platform-wide and do not align with per-restaurant access rules.

## 7. Next Steps
1. Confirm the exact KPIs required for the restaurant dashboard.
2. Decide whether revenue is derived from `Orders` totals or `PaymentTransactions` (refund-aware).
3. Add endpoint and query when ready to implement.
