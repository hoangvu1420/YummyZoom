# Workflow: TeamCart (Collaborative Ordering)

Guide for creating a shared TeamCart, inviting participants, collecting items and payments, and converting to an order.

## Overview

TeamCart enables group ordering with roles and a lightweight payment flow:
- Host creates a cart for a specific restaurant and shares a token
- Members join via share token and add their items
- Host locks the cart for payment; members commit payments (online or COD)
- When all members are settled, the host converts the cart to a standard order
- Real-time updates are available via SignalR

> See also
> For client payment confirmation flows, ETag polling, and real-time recommendations, read [Online Payments Integration](../06-Online-Payments-Integration.md).

Live tracking (MVP) uses a poll‑first endpoint with strong ETag to minimize payloads. Mobile push (FCM data) nudges clients to poll immediately when something changes.

All endpoints require `Authorization: Bearer <access_token>` unless stated. Feature is behind a flag; APIs can return `503 Service Unavailable` if disabled.

---

## Active Team Cart Summary

Get a lightweight summary of the user's currently active team cart (if any). Ideally used for widgets or home screen cards.

**`GET /api/v1/team-carts/active`**

- Authorization: Required (Customer)
- Response: 200 OK (with data) or 204 No Content (if no active cart)

#### Response
**✅ 200 OK**
```json
{
  "teamCartId": "c1a2b3d4-e5f6-7890-abcd-ef1234567890",
  "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "restaurantName": "Burger & Co",
  "restaurantImage": "https://example.com/logo.png",
  "state": "Open",
  "totalItemCount": 5,
  "myShareTotal": 20.50,
  "currency": "USD",
  "isHost": true,
  "createdAt": "2025-10-27T18:00:00Z"
}
```

**✅ 204 No Content**
(Empty body) - The user is not part of any active TeamCart.

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `teamCartId` | `string` | ID of the active cart |
| `restaurantId` | `string` | ID of the linked restaurant |
| `restaurantName` | `string` | Name of the restaurant |
| `restaurantImage` | `string` | URL of the restaurant logo (optional) |
| `state` | `string` | Current state: `Open`, `Locked`, `ReadyToConfirm` |
| `totalItemCount` | `integer` | Total number of items in the cart (sum of quantities) |
| `myShareTotal` | `decimal` | The user's personal share of the cost |
| `currency` | `string` | Currency code |
| `isHost` | `boolean` | `true` if the current user is the host |
| `createdAt` | `string` | ISO 8601 date when cart was created |

#### Business Rules
- Returns the most relevant active cart if multiple exist (currently prioritized by creation date).
- Returns 204 if user has no active carts in `Open`, `Locked`, or `ReadyToConfirm` state.
- `myShareTotal` reflects the quoted amount if locked, or a raw sum of user's items if open.

---

## Step 1 — Create a TeamCart (Host)

Creates a new collaborative cart for a specific restaurant and returns identifiers and a share token for others to join.

**`POST /api/v1/team-carts`**

- Authorization: Required (Customer)
- Idempotency: Supported via `Idempotency-Key` header

#### Request Body

```json
{
  "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "hostName": "Alex",
  "deadlineUtc": "2025-10-01T18:30:00Z"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `restaurantId` | `string` | Yes | UUID of the restaurant for this TeamCart |
| `hostName` | `string` | Yes | Display name for the host (1-100 characters) |
| `deadlineUtc` | `string` | No | ISO 8601 deadline for cart completion (must be in future) |

#### Response

**✅ 201 Created**
```json
{
  "teamCartId": "c1a2b3d4-e5f6-7890-abcd-ef1234567890",
  "shareToken": "XYZ123",
  "shareTokenExpiresAtUtc": "2025-10-01T18:30:00Z"
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `teamCartId` | `string` | UUID of the created TeamCart |
| `shareToken` | `string` | 6-character alphanumeric token for members to join |
| `shareTokenExpiresAtUtc` | `string` | ISO 8601 expiration time for the share token |

#### Business Rules

- Restaurant must exist and be active
- Host must be authenticated; automatically added as member with role "Host"
- `deadlineUtc` must be in the future if provided
- Share token expires 24 hours after creation
- TeamCart starts in `Open` state

#### Errors

- **400** `CreateTeamCart.InvalidHostName` - Host name validation failed
- **400** `CreateTeamCart.InvalidDeadline` - Deadline is in the past
- **404** `CreateTeamCart.RestaurantNotFound` - Restaurant doesn't exist
- **503** Service unavailable when TeamCart feature is disabled

---

## Step 2 — Join the TeamCart (Member)

Members join an existing TeamCart using the share token and provide a display name.

**`POST /api/v1/team-carts/{teamCartId}/join`**

- Authorization: Required (Customer)
- Path Parameter: `teamCartId` (UUID)

#### Request Body

```json
{
  "shareToken": "XYZ123",
  "guestName": "Sam"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `shareToken` | `string` | Yes | 6-character alphanumeric share token from host |
| `guestName` | `string` | Yes | Display name for the member (1-100 characters) |

#### Response

**✅ 204 No Content** - Successfully joined the TeamCart

#### Business Rules

- Member name is required and must be unique within the cart
- Cart must be in `Open` state and not expired
- Share token must be valid and not expired
- Duplicate membership is rejected (user already a member)
- Member is added with role "Guest"

#### Errors

- **400** `JoinTeamCart.InvalidGuestName` - Guest name validation failed
- **400** `JoinTeamCart.InvalidShareToken` - Share token is invalid or expired
- **400** `JoinTeamCart.AlreadyMember` - User is already a member of this cart
- **404** `JoinTeamCart.TeamCartNotFound` - TeamCart doesn't exist
- **409** `JoinTeamCart.CartNotOpen` - Cart is not in Open state
- **409** `JoinTeamCart.CartExpired` - Cart has expired
- **503** Service unavailable when TeamCart feature is disabled

---

## Live Tracking (Poll‑First, FCM‑Triggered)

Track TeamCart state changes efficiently while the session is active. Clients poll a lightweight real‑time view endpoint and use ETag validators to avoid unnecessary payloads. FCM data messages from the backend act as a trigger to poll immediately after changes.

### Endpoint — TeamCart Real‑time View (Redis)

**`GET /api/v1/team-carts/{teamCartId}/rt`**

- Authorization: Required (TeamCart member or host)
- Path Parameter: `teamCartId` (UUID)
- Caching: Strong ETag based on VM `Version`

Response — 200 OK
```json
{
  "teamCart": {
    "cartId": "c1a2b3d4-e5f6-7890-abcd-ef1234567890",
    "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "status": "Open",
    "deadline": "2025-10-27T19:30:00Z",
    "expiresAt": "2025-10-27T20:00:00Z",
    "shareTokenMasked": "***XYZ",
    "tipAmount": 0.0,
    "tipCurrency": "USD",
    "couponCode": null,
    "discountAmount": 0.0,
    "discountCurrency": "USD",
    "subtotal": 25.0,
    "currency": "USD",
    "deliveryFee": 0.0,
    "taxAmount": 0.0,
    "total": 25.0,
    "cashOnDeliveryPortion": 0.0,
    "quoteVersion": 0,
    "version": 7,
    "members": [
      {
        "userId": "1e2d3c4b-5a69-…",
        "name": "Host",
        "role": "Host",
        "paymentStatus": "Pending",
        "committedAmount": 0.0,
        "onlineTransactionId": null,
        "quotedAmount": 0.0,
        "isReady": false
      }
    ],
    "items": [
      {
        "itemId": "f1e2d3c4-b5a6-…",
        "addedByUserId": "1e2d3c4b-5a69-…",
        "name": "Burger XL",
        "imageUrl": "https://example.com/images/burger-xl.jpg",
        "menuItemId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
        "quantity": 1,
        "basePrice": 10.0,
        "lineTotal": 10.0,
        "customizations": []
      }
    ]
  }
}
```

Headers (200)
- `ETag: "teamcart-<teamCartId>-v<Version>"` (strong)
- `Last-Modified: <http-date>`
- `Cache-Control: no-cache, must-revalidate`

Conditional Requests
- Send `If-None-Match` with the last ETag to check for changes.
- If unchanged → **304 Not Modified** (empty body).

Authorization rules
- Auth required; caller must be a member of the TeamCart. Unauthorized carts are masked as 404.

Notes
- `version` increases on every user‑visible VM mutation (members/items/coupon/tip/status/payments/quote update).
- `quoteVersion` increases on quote recomputation (primarily while Locked/financials) and is included for financial context, but ETag validation uses `version`.

### Push Trigger — FCM (Data‑Only)

On every TeamCart change, the backend sends a data‑only FCM message to active member devices:

```json
{ "teamCartId": "<uuid>", "version": 7 }
```

Platform hints
- Android: `priority=high`, collapse key `teamcart_<id>`, TTL ≈ 5 minutes
- iOS/APNs: `apns-push-type=background`, `content-available=1`, collapse id `teamcart_<id>`

### Client Guidance (MVP)

- On screen open or app foreground: call `GET /team-carts/{id}/rt` and store both the body and ETag.
- While active: poll `/{id}/rt` every 10–15s with `If-None-Match: <etag>`.
- On receiving FCM `{teamCartId, version}`:
  - If `version` ≤ local `version`, ignore.
  - Else, call `/{id}/rt` with `If-None-Match` and update if 200; ignore if 304.
- Stop polling after terminal states: `Converted` or `Expired`.

Activity messages (client‑side diff, optional)
- Derive messages by diffing the last VM snapshot vs new VM:
  - New member → “{name} joined”
  - Item added/removed/quantity changed → “{actor} added/removed/updated {item}”
  - Coupon/tip toggled → “Host applied/removed coupon {code}” / “Host set tip to {amount}”
  - Status transitions → banners (Locked/ReadyToConfirm/Converted/Expired)

---

## Step 3 — Add Items to TeamCart (Members)

Members can add menu items with optional customizations to the TeamCart.

**`POST /api/v1/team-carts/{teamCartId}/items`**

- Authorization: Required (TeamCart member)
- Idempotency: Supported via `Idempotency-Key` header
- Path Parameter: `teamCartId` (UUID)

#### Request Body

```json
{
  "menuItemId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
  "quantity": 2,
  "selectedCustomizations": [
    {
      "groupId": "g1",
      "choiceId": "c1"
    }
  ]
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `menuItemId` | `string` | Yes | UUID of the menu item to add |
| `quantity` | `integer` | Yes | Quantity to add (1-99) |
| `selectedCustomizations` | `array` | No | Array of customization selections |

#### Customization Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `groupId` | `string` | Yes | UUID of the customization group |
| `choiceId` | `string` | Yes | UUID of the selected choice within the group |

#### Response

**✅ 201 Created**
```json
{
  "teamCartItemId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `teamCartItemId` | `string` | UUID of the created TeamCart item |

#### Business Rules

- Cart must be in `Open` state
- Caller must be a TeamCart member
- Menu item must belong to the same restaurant as the cart
- Menu item must be available for ordering
- Customization groups and choices must be valid for the menu item
- Quantity must be between 1 and 99

#### Errors

- **400** `AddItemToTeamCart.InvalidQuantity` - Quantity is out of range
- **400** `AddItemToTeamCart.MenuItemUnavailable` - Menu item is not available
- **400** `AddItemToTeamCart.CustomizationGroupNotApplied` - Customization group not valid for item
- **400** `AddItemToTeamCart.CustomizationChoiceNotValid` - Customization choice not valid for group
- **404** `AddItemToTeamCart.MenuItemNotFound` - Menu item doesn't exist
- **404** `AddItemToTeamCart.CustomizationGroupNotFound` - Customization group doesn't exist
- **404** `AddItemToTeamCart.CustomizationChoiceNotFound` - Customization choice doesn't exist
- **404** `AddItemToTeamCart.TeamCartNotFound` - TeamCart doesn't exist
- **409** `AddItemToTeamCart.CartNotOpen` - Cart is not in Open state
- **403** `AddItemToTeamCart.NotMember` - User is not a member of this cart

---

## Step 4 — Set Member Ready Status (Member)

Members can mark themselves as "ready" to indicate they have finished adding items. This helps the host know when to lock the cart.

**`POST /api/v1/team-carts/{teamCartId}/ready`**

- Authorization: Required (TeamCart member)
- Path Parameter: `teamCartId` (UUID)

#### Request Body

```json
{
  "isReady": true
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `isReady` | `boolean` | Yes | Whether the member is ready (true) or not (false) |

#### Response

**✅ 204 No Content** - Status updated successfully

#### Business Rules

- Cart must be in `Open` state
- Caller must be a TeamCart member
- Setting `isReady` to `true` signals completion of ordering
- Setting `isReady` to `false` signals still deciding

#### Errors

- **404** `SetMemberReady.TeamCartNotFound` - TeamCart doesn't exist
- **409** `SetMemberReady.CartNotOpen` - Cart is not in Open state
- **403** `SetMemberReady.NotMember` - User is not a member of this cart

---

## Step 5 — Get Live Cart State

Retrieve the current state of the TeamCart for real-time updates and member coordination.

### Get Cart Details (SQL)

**`GET /api/v1/team-carts/{teamCartId}`**

- Authorization: Required (TeamCart member)
- Path Parameter: `teamCartId` (UUID)

#### Response

**✅ 200 OK**
```json
{
  "id": "c1a2b3d4-e5f6-7890-abcd-ef1234567890",
  "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Open",
  "hostUserId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
  "deadlineUtc": "2025-10-01T18:30:00Z",
  "createdAtUtc": "2025-09-15T10:30:00Z",
  "members": [
    {
      "userId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
      "name": "Alex",
      "role": "Host"
    }
  ],
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "menuItemId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
      "ownerUserId": "b2c3d4e5-f6g7-8901-bcde-f23456789012",
      "quantity": 2,
      "unitPrice": 12.99,
      "totalPrice": 25.98
    }
  ]
}
```

### Real-time Updates via SignalR

**WebSocket Connection**: `wss://<host>/hubs/teamcart`

#### Subscribe to Cart Updates

```javascript
// Subscribe to cart updates
connection.invoke("SubscribeToCart", teamCartId);
```

#### Real-time Event Types

| Event | Description | Payload |
|-------|-------------|---------|
| `CartUpdated` | Items added/removed/updated | Cart view model |
| `Locked` | Cart locked for payment | Lock notification |
| `PaymentEvent` | Member payment status changed | Payment details |
| `ReadyToConfirm` | All members settled | Ready state |
| `Converted` | Cart converted to order | Order details |
| `Expired` | Cart expired | Expiration details |

#### Business Rules

- Membership enforced; non-members receive authorization errors
- Real-time view model is optimized for UI updates
- SQL endpoint provides complete cart details for administrative purposes
- SignalR provides immediate updates for collaborative experience

#### Errors

- **403** `GetTeamCartRealTimeViewModel.NotMember` - User is not a member of this cart
- **404** `GetTeamCartRealTimeViewModel.TeamCartNotFound` - TeamCart doesn't exist
- **503** Service unavailable when real-time features are disabled

---

## Step 6 — Lock Cart for Payment (Host)

Freezes items and computes per-member quotes. Notifies members to pay/commit.

**`POST /api/v1/team-carts/{teamCartId}/lock`**

- Authorization: Host only
- Idempotency: Supported via `Idempotency-Key` header
- Response: 200 OK

#### Response

**✅ 200 OK**
```json
{
  "quoteVersion": 1
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `quoteVersion` | `long` | Quote version for concurrency control. Include this in payment and conversion requests. |

Rules
- Only in `Open` state; transitions to `Locked`.
- Computes quote (member subtotals, fees, tip, tax, discount); updates VM.
- Returns quote version for concurrency control in subsequent operations.

Errors
- 400/409 domain validations; 404 `TeamCartErrors.TeamCartNotFound`

---

## Step 7 — Discover Coupons (Members)

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

## Step 8 — Apply Tip and Coupon (Host/Participant)

Apply tips and coupons to the TeamCart to adjust the final pricing.

### Apply Tip

**`POST /api/v1/team-carts/{teamCartId}/tip`**

- Authorization: Required (TeamCart participant - member or host)
- Path Parameter: `teamCartId` (UUID)

#### Request Body

```json
{
  "tipAmount": 5.00
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tipAmount` | `decimal` | Yes | Tip amount in cart currency (0.00 - 999.99) |

#### Response

**✅ 204 No Content** - Tip applied successfully

#### Business Rules

- Cart must be in `Open` or `Locked` state
- Caller must be a TeamCart participant (member or host)
- Tip amount must be non-negative
- Tip is distributed proportionally among all members

#### Errors

- **400** `ApplyTipToTeamCart.InvalidTipAmount` - Tip amount is negative or invalid
- **404** `ApplyTipToTeamCart.TeamCartNotFound` - TeamCart doesn't exist
- **409** `ApplyTipToTeamCart.CartNotOpenOrLocked` - Cart is not in valid state for tip application
- **403** `ApplyTipToTeamCart.NotParticipant` - User is not a participant in this cart

### Apply Coupon

**`POST /api/v1/team-carts/{teamCartId}/coupon`**

- Authorization: Required (Host only)
- Path Parameter: `teamCartId` (UUID)

#### Request Body

```json
{
  "couponCode": "WELCOME10"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `couponCode` | `string` | Yes | Coupon code to apply (1-50 characters) |

#### Response

**✅ 204 No Content** - Coupon applied successfully

#### Business Rules

- Cart must be in `Open` or `Locked` state
- Only the host can apply coupons
- Coupon must be valid and applicable to cart items
- Coupon is validated against current cart contents

#### Errors

- **400** `ApplyCouponToTeamCart.InvalidCouponCode` - Coupon code is invalid format
- **404** `ApplyCouponToTeamCart.CouponNotFound` - Coupon doesn't exist
- **404** `ApplyCouponToTeamCart.TeamCartNotFound` - TeamCart doesn't exist
- **409** `ApplyCouponToTeamCart.CouponNotApplicable` - Coupon not valid for cart items
- **409** `ApplyCouponToTeamCart.CartNotOpenOrLocked` - Cart is not in valid state
- **403** `ApplyCouponToTeamCart.NotHost` - User is not the host of this cart

### Remove Coupon

**`DELETE /api/v1/team-carts/{teamCartId}/coupon`**

- Authorization: Required (Host only)
- Path Parameter: `teamCartId` (UUID)

#### Response

**✅ 204 No Content** - Coupon removed successfully

#### Business Rules

- Only the host can remove coupons
- Cart must be in `Open` or `Locked` state
- If no coupon is applied, operation succeeds (idempotent)

#### Errors

- **404** `RemoveCouponFromTeamCart.TeamCartNotFound` - TeamCart doesn't exist
- **403** `RemoveCouponFromTeamCart.NotHost` - User is not the host of this cart

---

## Step 9 — Member Payments

Members settle their share via one of:

### Cash on Delivery Payment

**`POST /api/v1/team-carts/{teamCartId}/payments/cod`**

- Authorization: Member
- Response: 204 No Content

#### Request Body (Optional)

```json
{
  "quoteVersion": 1
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `quoteVersion` | `long` | No | Quote version from lock response. Validates against current quote to prevent stale payments. |

### Online Payment

**`POST /api/v1/team-carts/{teamCartId}/payments/online`**

- Authorization: Member
- Response: 200 OK

#### Request Body (Optional)

```json
{
  "quoteVersion": 1
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `quoteVersion` | `long` | No | Quote version from lock response. Validates against current quote to prevent stale payments. |

#### Response

**✅ 200 OK**
```json
{
  "paymentIntentId": "pi_...",
  "clientSecret": "pi_..._secret_..."
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `paymentIntentId` | `string` | Stripe PaymentIntent ID for tracking |
| `clientSecret` | `string` | Client secret for Stripe payment confirmation |

Rules
- Only members can pay; amounts must match their quoted share.
- System tracks per-member payment status; when everyone is settled, cart becomes `ReadyToConfirm`.
- Quote version validation prevents payments against stale quotes.

> See also
> Client steps to collect card details and confirm the Stripe Payment Intent using the returned `clientSecret` are detailed in [Online Payments Integration](../06-Online-Payments-Integration.md).

Errors
- 400 invalid payment amount; 404 cart not found
- 409 `TeamCart.QuoteVersionMismatch` when quote version doesn't match current version

---

## Step 10 — Convert to Order (Host)

After all members are settled, convert the cart to a standard order.

**`POST /api/v1/team-carts/{teamCartId}/convert`**

- Authorization: Host only
- Idempotency: Supported via `Idempotency-Key` header

#### Request Body

```json
{
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "US",
  "specialInstructions": "Leave at lobby",
  "quoteVersion": 1
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `street` | `string` | Yes | Delivery street address |
| `city` | `string` | Yes | Delivery city |
| `state` | `string` | Yes | Delivery state/province |
| `zipCode` | `string` | Yes | Delivery postal code |
| `country` | `string` | Yes | Delivery country code |
| `specialInstructions` | `string` | No | Special delivery instructions |
| `quoteVersion` | `long` | No | Quote version from lock response. Validates against current quote to prevent conversion with stale data. |

#### Response

**✅ 200 OK**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `orderId` | `string` | UUID of the created order |

Rules
- Cart must be `ReadyToConfirm`; converts to terminal `Converted` state.
- Validates paid sum equals grand total; computes fees/taxes as per policy.
- Quote version validation prevents conversion with stale quote data.

Errors
- 400 `TeamCartErrors.InvalidPaymentAmount`
- 409 `TeamCartErrors.InvalidStatusForConversion`
- 409 `TeamCart.QuoteVersionMismatch` when quote version doesn't match current version

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
- Quote Versioning
  - Lock operation returns a quote version for concurrency control.
  - Payment and conversion operations can include quote version for validation.
  - Prevents payments against stale quotes when cart is modified concurrently.
  - Returns 409 `TeamCart.QuoteVersionMismatch` when version doesn't match current quote.
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
