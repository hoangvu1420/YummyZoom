# Coupon Management (Provider)

Base path: `/api/v1/`

These endpoints let restaurant staff create and manage coupons, fetch details and usage stats, and list coupons with filters. A separate fast-check endpoint supports cart-time evaluation.

Authorization:
- All endpoints under `/restaurants/{restaurantId}/coupons` require `MustBeRestaurantStaff` for that restaurant.
- Fast-check also requires authentication (protected group).

---

## List Coupons

### GET /restaurants/{restaurantId}/coupons?PageNumber=1&PageSize=20&Q=SUMMER&Enabled=true&From=2025-06-01&To=2025-08-31

Returns paginated coupons with optional filters by code/description text, enabled status, and validity window.

- Authorization: MustBeRestaurantStaff

#### Path Parameters
- `restaurantId` (UUID) — Restaurant scope.

#### Query Parameters
- `PageNumber` (int, default 1, >= 1)
- `PageSize` (int, default 20, 1..100)
- `Q` (string, optional, max 200) — searches code/description
- `Enabled` (bool, optional)
- `From` (ISO date, optional) — validity start filter (inclusive)
- `To` (ISO date, optional) — validity end filter (inclusive). If both supplied, `From <= To`.

#### Response 200
Paginated list of coupon summaries (fields include `couponId`, `code`, `description`, value fields, scope, validity dates, min order, usage limits, `isEnabled`, `created`, `lastModified`).

#### Errors
- 400 Validation — invalid paging or date window

---

## Get Coupon Details

### GET /restaurants/{restaurantId}/coupons/{couponId}

- Authorization: MustBeRestaurantStaff

#### Response 200
Includes full coupon data with applies-to identifiers:

```json
{
  "couponId": "08f72a5a-...",
  "code": "SUMMER15",
  "description": "15% off",
  "valueType": "Percentage",
  "percentage": 15,
  "fixedAmount": null,
  "fixedCurrency": null,
  "freeItemId": null,
  "scope": "WholeOrder",
  "itemIds": [],
  "categoryIds": [],
  "validityStartDate": "2025-06-01T00:00:00Z",
  "validityEndDate": "2025-08-31T23:59:59Z",
  "minOrderAmount": null,
  "minOrderCurrency": null,
  "totalUsageLimit": 1000,
  "currentTotalUsageCount": 125,
  "usageLimitPerUser": 3,
  "isEnabled": true,
  "created": "2025-05-01T10:00:00Z",
  "lastModified": "2025-06-15T09:20:00Z"
}
```

#### Errors
- 404 Not Found — `Coupon.Details.NotFound`

---

## Get Coupon Usage Stats

### GET /restaurants/{restaurantId}/coupons/{couponId}/stats

- Authorization: MustBeRestaurantStaff

#### Response 200
```json
{ "totalUsage": 125, "uniqueUsers": 92, "lastUsedAtUtc": "2025-08-12T17:30:00Z" }
```

#### Errors
- 404 Not Found — `Coupon.Stats.NotFound`

---

## Create Coupon

### POST /restaurants/{restaurantId}/coupons

Create a new coupon for the restaurant.

- Authorization: MustBeRestaurantStaff

#### Request Body
```json
{
  "code": "FALL10",
  "description": "10% off fall menu",
  "valueType": "Percentage",
  "percentage": 10,
  "fixedAmount": null,
  "fixedCurrency": null,
  "freeItemId": null,
  "scope": "WholeOrder",
  "itemIds": null,
  "categoryIds": null,
  "validityStartDate": "2025-09-01T00:00:00Z",
  "validityEndDate": "2025-10-31T23:59:59Z",
  "minOrderAmount": 20.0,
  "minOrderCurrency": "USD",
  "totalUsageLimit": 1000,
  "usageLimitPerUser": 2,
  "isEnabled": true
}
```

Field rules (validators + domain):
- Value types:
  - `Percentage`: require `percentage` in (0, 100]; ignore other value fields
  - `FixedAmount`: require `fixedAmount > 0` and `fixedCurrency`
  - `FreeItem`: require `freeItemId` (UUID of a menu item)
- Scope:
  - `WholeOrder`: no IDs required
  - `SpecificItems`: `itemIds` must be non-empty; each item must belong to this restaurant
  - `SpecificCategories`: `categoryIds` must be non-empty; each category must belong to this restaurant
- Validity: `validityStartDate < validityEndDate`
- Minimum order: if `minOrderAmount` is set, must be > 0 and `minOrderCurrency` required
- Usage limits: if provided, `totalUsageLimit > 0`, `usageLimitPerUser > 0`
- Text limits: `description` max 500; `code` max 50

#### Response 201
```json
{ "couponId": "08f72a5a-..." }
```

#### Errors
- 400 Validation — value type/fields mismatch, scope IDs missing/invalid, min order rules, date order, text limits
- 403 Forbidden — cross-restaurant IDs or wrong scope

---

## Update Coupon

### PUT /restaurants/{restaurantId}/coupons/{couponId}

Update description, validity window, value, scope, min order, and usage limits.

- Authorization: MustBeRestaurantStaff

#### Request Body
```json
{
  "description": "Extended fall promo",
  "validityStartDate": "2025-09-01T00:00:00Z",
  "validityEndDate": "2025-11-15T23:59:59Z",
  "valueType": "FixedAmount",
  "percentage": null,
  "fixedAmount": 5.00,
  "fixedCurrency": "USD",
  "freeItemId": null,
  "scope": "SpecificCategories",
  "itemIds": null,
  "categoryIds": ["3f3d...", "7a2c..."],
  "minOrderAmount": null,
  "minOrderCurrency": null,
  "totalUsageLimit": 1500,
  "usageLimitPerUser": 3
}
```

Rules mirror creation; in addition, the handler verifies all referenced items/categories belong to the same restaurant. Setting `minOrderAmount` to null clears the minimum.

#### Response
- 204 No Content

#### Errors
- 404 Not Found — `Coupon.NotFound`
- 400 Validation — invalid combos (`Coupon.ValueTypeInvalid`, `Coupon.ScopeInvalid`, `Coupon.ItemIdsRequired`, `Coupon.CategoryIdsRequired`, etc.)
- 403 Forbidden — coupon not in `restaurantId`, or referenced resources not in restaurant

---

## Enable/Disable Coupon

### PUT /restaurants/{restaurantId}/coupons/{couponId}/enable
### PUT /restaurants/{restaurantId}/coupons/{couponId}/disable

Toggle coupon availability.

- Authorization: MustBeRestaurantStaff

#### Response
- 204 No Content

#### Errors
- 404 Not Found — `Coupon.NotFound`
- 403 Forbidden — coupon not in `restaurantId`

---

## Delete Coupon

### DELETE /restaurants/{restaurantId}/coupons/{couponId}

Soft-deletes a coupon.

- Authorization: MustBeRestaurantStaff

#### Response
- 204 No Content

#### Errors
- 404 Not Found — `Coupon.NotFound`
- 403 Forbidden — coupon not in `restaurantId`

---

## Fast Coupon Check (Cart-time)

### POST /coupons/fast-check

Evaluate coupon candidates quickly for a cart snapshot.

- Authorization: Authenticated user (protected group)

#### Request Body
```json
{
  "restaurantId": "8b8c...",
  "items": [
    { "menuItemId": "m1...", "menuCategoryId": "c1...", "qty": 2, "unitPrice": 9.99 },
    { "menuItemId": "m2...", "menuCategoryId": "c2...", "qty": 1, "unitPrice": 4.50 }
  ]
}
```

#### Response 200
```json
{
  "bestDeal": {
    "code": "FALL10",
    "label": "10% off fall menu",
    "savings": 2.95,
    "meetsMinOrder": true,
    "minOrderGap": 0,
    "validityEnd": "2025-10-31T23:59:59Z",
    "scope": "WholeOrder",
    "reasonIfIneligible": null
  },
  "candidates": []
}
```

Notes
- The API surfaces candidates and a computed best deal; eligibility considers validity, scope, and minimum order.
- Client apps can present reasons for ineligibility and nudge users to meet minima.

---

## Business Rules Summary

- Value Types
  - Percentage: 0 < p ≤ 100
  - FixedAmount: amount > 0 with currency
  - FreeItem: requires a valid menu item ID
- Scope
  - WholeOrder: applies to entire order
  - SpecificItems: non-empty `itemIds` and must belong to the restaurant
  - SpecificCategories: non-empty `categoryIds` and must belong to the restaurant
- Validity window: start < end; list filters accept `From`/`To` with `From <= To`.
- Minimum order: if set, amount > 0 with currency; can be cleared in update by omitting amount (handler removes it).
- Usage limits: positive integers when provided.
- Security: All operations are restaurant-scoped and enforce tenancy before state changes.

Versioning: All routes use `/api/v1/`.

