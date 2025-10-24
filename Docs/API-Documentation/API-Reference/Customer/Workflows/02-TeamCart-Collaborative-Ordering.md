# Workflow: TeamCart (Collaborative Ordering)

Guide for creating a shared TeamCart, inviting participants, collecting items and payments, and converting to an order.

## Overview

TeamCart enables group ordering with roles and a lightweight payment flow:
- Host creates a cart for a specific restaurant and shares a token
- Members join via share token and add their items
- Host locks the cart for payment; members commit payments (online or COD)
- When all members are settled, the host converts the cart to a standard order
- Real-time updates are available via SignalR

All endpoints require `Authorization: Bearer <access_token>` unless stated. Feature is behind a flag; APIs can return `503 Service Unavailable` if disabled.

---

## Step 1 — Create a TeamCart (Host)

Creates a new cart and returns identifiers and a share token for others to join.

**`POST /api/v1/team-carts`**

- Authorization: Required (Customer)
- Idempotency: Supported via `Idempotency-Key` header
- Body
```json
{ "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "hostName": "Alex", "deadlineUtc": "2025-10-01T18:30:00Z" }
```

Response — 201 Created
```json
{ "teamCartId": "c1a2b3d4-e5f6-7890-abcd-ef1234567890", "shareToken": "XYZ123", "shareTokenExpiresAtUtc": "2025-10-01T18:30:00Z" }
```

Rules
- Restaurant must exist.
- Host must be authenticated; host is added as a member with role "Host".
- Optional `deadlineUtc` must be in the future.

Errors
- 404 `CreateTeamCart.RestaurantNotFound`
- 503 when feature/realtime not available

---

## Step 2 — Join the TeamCart (Member)

Members join using the share token and provide a display name.

**`POST /api/v1/team-carts/{teamCartId}/join`**

- Authorization: Required
- Body
```json
{ "shareToken": "XYZ123", "guestName": "Sam" }
```

Response — 204 No Content

Rules
- Member name required.
- Cart must be `Open` and not expired; duplicate membership rejected.

Typical errors
- 400 `GetTeamCartRealTimeViewModel.NotMember` (when attempting to fetch without joining)
- 400/409 member validation errors; 503 feature disabled

---

## Step 3 — Add and Manage Items (Members)

Members can add items with optional customizations, update their quantities, or remove their own items.

- Add item — `POST /api/v1/team-carts/{teamCartId}/items` (supports `Idempotency-Key` header)
  ```json
  {
    "menuItemId": "b2c3...",
    "quantity": 2,
    "selectedCustomizations": [ { "groupId": "g1", "choiceId": "c1" } ]
  }
  ```
- Update quantity — `POST /api/v1/team-carts/{teamCartId}/items/{teamCartItemId}`
  ```json
  { "newQuantity": 3 }
  ```
- Remove item — `DELETE /api/v1/team-carts/{teamCartId}/items/{teamCartItemId}`

Rules
- Cart must be `Open`.
- Caller must be a member; only the owner of an item may update/remove it.
- Menu item must belong to the same restaurant and be available; customization groups/choices must be valid for the item.

Error codes (examples)
- 404 `AddItemToTeamCart.MenuItemNotFound`, `CustomizationGroupNotFound`, `CustomizationChoiceNotFound`, `UpdateTeamCartItemQuantity.ItemNotFound`
- 400 `AddItemToTeamCart.MenuItemUnavailable`, `CustomizationGroupNotApplied`, `UpdateTeamCartItemQuantity.NotOwner`, `RemoveItemFromTeamCart.NotOwner`

---

## Step 4 — Get Live Cart State

- Details (SQL) — `GET /api/v1/team-carts/{teamCartId}`
- Real-time VM (Redis) — `GET /api/v1/team-carts/{teamCartId}/rt`
- SignalR hub subscription — `wss://<host>/hubs/teamcart` then `SubscribeToCart(teamCartId)`

Notes
- Membership enforced; non-members receive authorization errors.
- Real-time notifications include: `CartUpdated`, `Locked`, `PaymentEvent`, `ReadyToConfirm`, `Converted`, `Expired`.

---

## Step 5 — Lock Cart for Payment (Host)

Freezes items and computes per-member quotes. Notifies members to pay/commit.

**`POST /api/v1/team-carts/{teamCartId}/lock`**

- Authorization: Host only
- Idempotency: Supported via `Idempotency-Key` header
- Response: 204 No Content

Rules
- Only in `Open` state; transitions to `Locked`.
- Computes quote (member subtotals, fees, tip, tax, discount); updates VM.

Errors
- 400/409 domain validations; 404 `TeamCartErrors.TeamCartNotFound`

---

## Step 6 — Discover Coupons (Members)

Get real-time coupon suggestions and savings calculations for the current TeamCart items. This helps members and the host discover applicable coupons before applying them.

**`GET /api/v1/team-carts/{teamCartId}/coupon-suggestions`**

- Authorization: TeamCart member (host or guest)
- Response: 200 OK with coupon suggestions

#### Response

```json
{
  "cartSummary": {
    "subtotal": 45.97,
    "currency": "USD",
    "itemCount": 3
  },
  "bestDeal": {
    "code": "TEAM15",
    "label": "15% off entire order",
    "savings": 6.90,
    "isEligible": true,
    "eligibilityReason": null,
    "minOrderGap": 0,
    "expiresOn": "2025-11-15T23:59:59Z",
    "scope": "WholeOrder",
    "urgency": "None"
  },
  "suggestions": [
    {
      "code": "TEAM15",
      "label": "15% off entire order",
      "savings": 6.90,
      "isEligible": true,
      "eligibilityReason": null,
      "minOrderGap": 0,
      "expiresOn": "2025-11-15T23:59:59Z",
      "scope": "WholeOrder",
      "urgency": "None"
    },
    {
      "code": "BIGORDER",
      "label": "10% off orders over $100",
      "savings": 0,
      "isEligible": false,
      "eligibilityReason": "MinAmountNotMet",
      "minOrderGap": 54.03,
      "expiresOn": "2025-12-01T23:59:59Z",
      "scope": "WholeOrder",
      "urgency": "ExpiresWithin7Days"
    }
  ]
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `cartSummary` | `CartSummary` | Cart totals and item count |
| `bestDeal` | `CouponSuggestion?` | Highest-value eligible coupon (null if none) |
| `suggestions` | `CouponSuggestion[]` | All applicable coupons, sorted by value |

#### Cart Summary Schema

| Field | Type | Description |
|-------|------|-------------|
| `subtotal` | `decimal` | Total cart value before discounts |
| `currency` | `string` | Currency code |
| `itemCount` | `integer` | Total number of items |

#### Coupon Suggestion Schema

| Field | Type | Description |
|-------|------|-------------|
| `code` | `string` | Coupon code |
| `label` | `string` | Human-readable description |
| `savings` | `decimal` | Calculated savings amount |
| `isEligible` | `boolean` | Whether coupon can be applied |
| `eligibilityReason` | `string?` | Reason if not eligible (null if eligible) |
| `minOrderGap` | `decimal` | Additional amount needed for minimum order |
| `expiresOn` | `datetime` | Coupon expiration date |
| `scope` | `string` | Coupon scope: "WholeOrder", "SpecificItems", "SpecificCategories" |
| `urgency` | `string` | Urgency level: "None", "ExpiresWithin24Hours", "ExpiresWithin7Days", "LimitedUsesRemaining" |

#### Eligibility Reasons

| Reason | Description |
|--------|-------------|
| `MinAmountNotMet` | Cart total below minimum order requirement |
| `UsageLimitExceeded` | User or total usage limit reached |
| `Expired` | Coupon has expired |
| `NotYetValid` | Coupon not yet active |
| `Disabled` | Coupon is disabled |
| `NotApplicable` | Coupon doesn't apply to cart items |

#### Business Rules

- **Authorization**: User must be a member of the TeamCart (host or guest)
- **Performance**: Response optimized for <500ms using materialized views
- **Consistency**: Calculations match actual order financial service
- **Real-time**: Results reflect current cart state including all member items
- **Usage Limits**: Respects per-user and total usage limits

#### Error Responses

- **401 Unauthorized**: Missing or invalid authentication token
- **403 Forbidden**: User is not a member of the TeamCart
- **404 Not Found**: TeamCart not found
- **500 Internal Server Error**: System error during processing

---

## Step 7 — Apply Tip and Coupon (Host/Participant)

- Apply tip — `POST /api/v1/team-carts/{teamCartId}/tip`
  - Authorization: Participant (member or host)
  - Body: `{ "tipAmount": 5.00 }`
- Apply coupon — `POST /api/v1/team-carts/{teamCartId}/coupon`
  - Authorization: Host only
  - Body: `{ "couponCode": "WELCOME10" }`
- Remove coupon — `DELETE /api/v1/team-carts/{teamCartId}/coupon`

Rules
- Tip/coupon typically applied after locking. Coupon validated against cart items.

Errors (examples)
- 404 `Coupon.CouponNotFound`
- 400 validation when amounts/codes invalid

---

## Step 8 — Member Payments

Members settle their share via one of:

- Cash on Delivery commit — `POST /api/v1/team-carts/{teamCartId}/payments/cod`
  - Authorization: Member
  - Response: 204 No Content
- Online payment intent — `POST /api/v1/team-carts/{teamCartId}/payments/online`
  - Authorization: Member
  - Response 200 OK
    ```json
    { "paymentIntentId": "pi_...", "clientSecret": "pi_..._secret_..." }
    ```

Rules
- Only members can pay; amounts must match their quoted share.
- System tracks per-member payment status; when everyone is settled, cart becomes `ReadyToConfirm`.

Errors
- 400 invalid payment amount; 404 cart not found

---

## Step 9 — Convert to Order (Host)

After all members are settled, convert the cart to a standard order.

**`POST /api/v1/team-carts/{teamCartId}/convert`**

- Authorization: Host only
- Idempotency: Supported via `Idempotency-Key` header
- Body
```json
{
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "US",
  "specialInstructions": "Leave at lobby"
}
```

Response — 200 OK
```json
{ "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479" }
```

Rules
- Cart must be `ReadyToConfirm`; converts to terminal `Converted` state.
- Validates paid sum equals grand total; computes fees/taxes as per policy.

Errors
- 400 `TeamCartErrors.InvalidPaymentAmount`
- 409 `TeamCartErrors.InvalidStatusForConversion`

---

## States & Transitions

- `Open` → members join, add/remove items
- `Open` → `Locked` — host locks for payment
- `Locked` → `ReadyToConfirm` — all members settled (online success or COD commitments)
- `ReadyToConfirm` → `Converted` — host converts to order
- Any non-terminal → `Expired` — timeout or deadline passed (background service)

---

## Business Rules Summary

- Membership & Roles
  - Host only: create cart, lock, apply coupon, convert.
  - Member: join via valid token, add/manage own items, pay/commit.
  - Participant: members and host can apply tip.
- Item Rules
  - Only `Open` carts accept changes; item owner must perform updates/removals.
  - Items must belong to the cart's restaurant; customizations must be valid for the item.
- Payments
  - Per-member quotes computed at lock; amounts must match on commit.
  - All members must be settled before conversion.
- Idempotency Protection
  - Critical operations (create, add items, lock, convert) support `Idempotency-Key` header
  - Prevents duplicate actions from network retries or client errors
  - Keys are cached for 5 minutes after successful operation
- Feature Flags & Availability
  - Endpoints may return 503 if TeamCart or real-time store is disabled.

---

## Error Handling

Standard RFC 7807 problem details with 400/404/409/500 status codes. Common codes include:
- `CreateTeamCart.RestaurantNotFound`, `TeamCartErrors.TeamCartNotFound`
- `AddItemToTeamCart.MenuItemNotFound|Unavailable|Customization*`
- `UpdateTeamCartItemQuantity.NotOwner|ItemNotFound`, `RemoveItemFromTeamCart.NotOwner|ItemNotFound`
- `Coupon.CouponNotFound`, `TeamCartErrors.InvalidStatusForConversion`, `TeamCartErrors.InvalidPaymentAmount`

---

## Real-time Notes

- Subscribe to SignalR hub `/hubs/teamcart` and call `SubscribeToCart(teamCartId)` after join.
- Server broadcasts: `CartUpdated`, `Locked`, `PaymentEvent`, `ReadyToConfirm`, `Converted`, `Expired`.

---

## Next Steps

- For converting TeamCart to individual orders’ schema, see `../03-Individual-Orders.md`.
- For restaurant discovery before creating a TeamCart, see `../02-Restaurant-Discovery.md`.
