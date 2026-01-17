# Order Operations (Provider)

Base path: `/api/v1/`

These endpoints let restaurant staff operate orders: view actionable queues and drive lifecycle transitions from acceptance through delivery.

Authorization: Unless noted, endpoints require `MustBeRestaurantStaff` for the specified restaurant. Responses use standard helpers: 200 OK with body for reads and lifecycle results; 204 No Content for simple updates where applicable.

Cross‑refs: Real-time broadcasting of lifecycle changes is documented in Docs/API-Documentation/04-Real-time-Events-API.md. Payment webhooks that move orders from AwaitingPayment → Placed are covered in the Payments/Webhooks workflow.

---

## Restaurant Queues

### GET /restaurants/{restaurantId}/orders/new?pageNumber=1&pageSize=10

Returns orders with Status = `Placed`, oldest first (FIFO) for intake.

- Authorization: MustBeRestaurantStaff
- Response 200: Paginated list of `OrderSummaryDto` objects:
  - Fields: `orderId`, `orderNumber`, `status`, `placementTimestamp`, `restaurantId`, `customerId`, `totalAmount`, `totalCurrency`, `itemCount`, `sourceTeamCartId`, `isFromTeamCart`, `paidOnlineAmount`, `cashOnDeliveryAmount`.
- Errors: Standard validation on pagination.

### GET /restaurants/{restaurantId}/orders/active?pageNumber=1&pageSize=10

Returns orders with Status in {`Accepted`, `Preparing`, `ReadyForDelivery`}.

- Authorization: MustBeRestaurantStaff
- Response 200: Paginated list of `OrderSummaryDto`.

### GET /restaurants/{restaurantId}/orders/history?pageNumber=1&pageSize=10&from=&to=&statuses=&keyword=

Returns historical orders (terminal statuses) for reporting and customer support.

- Authorization: MustBeRestaurantStaff
- Filters (all optional):
  - `from`, `to` — ISO 8601 timestamps, filter by `placementTimestamp` range.
  - `statuses` — CSV of statuses (e.g., `Delivered,Cancelled,Rejected`).
  - `keyword` — order number or customer name/phone.
- Response 200: Paginated list of `OrderHistorySummaryDto` objects.

`OrderHistorySummaryDto` fields
- `orderId`, `orderNumber`, `status`, `placementTimestamp`, `completedTimestamp`
- `totalAmount`, `totalCurrency`, `itemCount`
- `customerName`, `customerPhone`
- `paymentStatus` (e.g., `Paid`, `Pending`, `Failed`)
- `paymentMethod` (e.g., `CashOnDelivery`, `CreditCard`)
- `sourceTeamCartId`, `isFromTeamCart`
- `paidOnlineAmount`, `cashOnDeliveryAmount` (payment split; prefer these over `paymentMethod` for TeamCart-originated orders)

Example response item

```json
{
  "orderId": "f47ac10b-...",
  "orderNumber": "ORD-20250914-102210-1234",
  "status": "Delivered",
  "placementTimestamp": "2025-09-14T10:20:00Z",
  "completedTimestamp": "2025-09-14T11:25:10Z",
  "totalAmount": 170000,
  "totalCurrency": "VND",
  "itemCount": 3,
  "customerName": "Nguyen Van A",
  "customerPhone": "09xxxx",
  "paymentStatus": "Paid",
  "paymentMethod": "CashOnDelivery",
  "sourceTeamCartId": null,
  "isFromTeamCart": false,
  "paidOnlineAmount": 0,
  "cashOnDeliveryAmount": 170000
}
```

---

## Order Detail (Live Orders)

### GET /restaurants/{restaurantId}/orders/{orderId}

Returns a detailed view of a single order for kitchen/ops screens (line items + selected customizations + monetary breakdown).

- Authorization: `MustBeRestaurantStaff`
- Tenancy: `orderId` must belong to `restaurantId`; otherwise returns 404 (not found).

#### Response 200 (OrderDetailsDto)

```json
{
  "orderId": "f47ac10b-...",
  "orderNumber": "ORD-20250914-102210-1234",
  "customerId": "c1d2...",
  "restaurantId": "b1c2...",
  "status": "Placed",
  "placementTimestamp": "2025-09-14T10:20:00Z",
  "lastUpdateTimestamp": "2025-09-14T10:22:10Z",
  "estimatedDeliveryTime": "2025-09-14T11:00:00Z",
  "actualDeliveryTime": null,
  "note": "Giao trước cổng, gọi trước 5 phút",
  "currency": "VND",
  "subtotalAmount": 170000,
  "discountAmount": 0,
  "deliveryFeeAmount": 0,
  "tipAmount": 0,
  "taxAmount": 0,
  "totalAmount": 170000,
  "sourceTeamCartId": null,
  "isFromTeamCart": false,
  "paymentMethod": "CashOnDelivery",
  "paidOnlineAmount": 0,
  "cashOnDeliveryAmount": 170000,
  "items": [
    {
      "orderItemId": "d7c9...",
      "menuItemId": "m1...",
      "name": "Burger bò",
      "quantity": 2,
      "unitPriceAmount": 65000,
      "lineItemTotalAmount": 130000,
      "customizations": [
        { "groupName": "Topping", "choiceName": "Thêm phô mai", "priceAdjustmentAmount": 10000 }
      ],
      "imageUrl": "https://cdn.example.com/menu/burger.jpg"
    }
  ]
}
```

Notes
- `note` is the customer’s order-level special instructions (optional).
- Per-item notes are not supported in v1; use item customizations for “no onions / less spicy” style preferences.

---

## Lifecycle Actions

Common behavior
- All actions are restaurant-scoped; if the order’s `restaurantId` doesn’t match the body’s `restaurantId`, the request is forbidden.
- Idempotency: If the order is already at the target status, handlers return the current `OrderLifecycleResultDto` without error.
- On success, a lifecycle event is emitted and real-time notifications may be broadcast to customer and/or restaurant listeners.

`OrderLifecycleResultDto` (response body for actions)

````json
{
  "orderId": "f47ac10b-...",
  "orderNumber": "ORD-20250914-102210-1234",
  "status": "Accepted",
  "placementTimestamp": "2025-09-14T10:20:00Z",
  "lastUpdateTimestamp": "2025-09-14T10:22:10Z",
  "estimatedDeliveryTime": "2025-09-14T11:00:00Z",
  "actualDeliveryTime": null
}
````

### POST /orders/{orderId}/accept

Accept a placed order and set the estimated delivery time.

- Body
  ```json
  { "restaurantId": "b1c2...", "estimatedDeliveryTime": "2025-09-14T11:00:00Z" }
  ```
- Preconditions: Status must be `Placed`.
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `AcceptOrder.NotFound`
  - 400 Validation — `Order.InvalidOrderStatusForAccept`
  - 403 Forbidden — restaurant mismatch

### POST /orders/{orderId}/reject

Reject a placed order.

- Body
  ```json
  { "restaurantId": "b1c2...", "reason": "Kitchen at capacity" }
  ```
- Preconditions: Status must be `Placed`.
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `RejectOrder.NotFound`
  - 400 Validation — `Order.InvalidStatusForReject`
  - 403 Forbidden — restaurant mismatch

### POST /orders/{orderId}/cancel

Cancel an order. Staff may cancel in multiple stages; customers are more restricted.

- Body
  ```json
  { "restaurantId": "b1c2...", "actingUserId": "(optional)", "reason": "Customer request" }
  ```
- Preconditions:
  - Staff/Admin: Status in {`Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`}.
  - Customer actor: only from `Placed` or `Accepted` (otherwise `CancelOrder.InvalidStatusForCustomer`).
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `CancelOrder.NotFound`
  - 400 Validation — `CancelOrder.InvalidStatusForCustomer` (when actor is customer)
  - 403 Forbidden — lacks permission or restaurant mismatch

### POST /orders/{orderId}/preparing

Mark an accepted order as preparing.

- Body
  ```json
  { "restaurantId": "b1c2..." }
  ```
- Preconditions: Status must be `Accepted`.
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `MarkOrderPreparing.NotFound`
  - 400 Validation — `Order.InvalidOrderStatusForPreparing`
  - 403 Forbidden — restaurant mismatch

### POST /orders/{orderId}/ready

Mark a preparing order as ready for delivery.

- Body
  ```json
  { "restaurantId": "b1c2..." }
  ```
- Preconditions: Status must be `Preparing`.
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `MarkOrderReadyForDelivery.NotFound`
  - 400 Validation — `Order.InvalidOrderStatusForReadyForDelivery`
  - 403 Forbidden — restaurant mismatch

### POST /orders/{orderId}/delivered

Mark a ready order as delivered; optionally set the delivered timestamp.

- Body
  ```json
  { "restaurantId": "b1c2...", "deliveredAtUtc": "2025-09-14T11:25:10Z" }
  ```
- Preconditions: Status must be `ReadyForDelivery`.
- Side effect: Sets `actualDeliveryTime` (uses provided timestamp or current UTC).
- Response 200: `OrderLifecycleResultDto`
- Errors:
  - 404 Not Found — `MarkOrderDelivered.NotFound`
  - 400 Validation — `Order.InvalidOrderStatusForDelivered`
  - 403 Forbidden — restaurant mismatch

---

## Status Reference (Domain)

Allowed lifecycle transitions (subset):
- `Placed` → `Accepted` (Accept)
- `Placed` → `Rejected` (Reject)
- `Accepted` → `Preparing` (MarkPreparing)
- `Preparing` → `ReadyForDelivery` (MarkReadyForDelivery)
- `ReadyForDelivery` → `Delivered` (MarkDelivered)
- Cancel: from `Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`

Other transitions
- Payment webhook: `AwaitingPayment` → `Placed` on success, or → `Cancelled` on failure.

---

## Error Codes (selected)

- `Order.InvalidOrderStatusForAccept`
- `Order.InvalidStatusForReject`
- `Order.InvalidOrderStatusForPreparing`
- `Order.InvalidOrderStatusForReadyForDelivery`
- `Order.InvalidOrderStatusForDelivered`
- `CancelOrder.InvalidStatusForCustomer`

Standard 401/403/404/409 responses follow platform conventions.

Versioning: All routes use `/api/v1/`.
