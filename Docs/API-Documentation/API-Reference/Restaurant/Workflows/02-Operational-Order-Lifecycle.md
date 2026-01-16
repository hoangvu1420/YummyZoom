# Workflow: Operational Order Lifecycle (Provider)

This guide covers day-to-day order operations for restaurant staff: triaging new orders, progressing orders through the kitchen, handling exceptions, and completing delivery.

—

## Overview

- Core statuses: `Placed` → `Accepted` → `Preparing` → `ReadyForDelivery` → `Delivered`.
- Exceptions: `Reject` (from Placed), `Cancel` (from Placed/Accepted/Preparing/ReadyForDelivery).
- Payments: Webhooks transition `AwaitingPayment` → `Placed` (success) or → `Cancelled` (failure).
- Authorization: All operations below require `MustBeRestaurantStaff` for the target `restaurantId`.

—

## 1) Triage Queues (Restaurant)

### GET /api/v1/restaurants/{restaurantId}/orders/new?pageNumber=1&pageSize=10
- Authorization: MustBeRestaurantStaff.
- Description: FIFO list of newly `Placed` orders for intake.
- Response 200: Paginated list of OrderSummaryDto
  - Fields: `orderId`, `orderNumber`, `status`, `placementTimestamp`, `restaurantId`, `customerId`, `totalAmount`, `totalCurrency`, `itemCount`, `sourceTeamCartId`, `isFromTeamCart`, `paidOnlineAmount`, `cashOnDeliveryAmount`.

### GET /api/v1/restaurants/{restaurantId}/orders/active?pageNumber=1&pageSize=10
- Authorization: MustBeRestaurantStaff.
- Description: Orders currently `Accepted`, `Preparing`, or `ReadyForDelivery`.
- Response 200: Paginated list of OrderSummaryDto.

Best practice
- Poll these queues only as a fallback. Prefer real-time subscriptions; see Docs/API-Documentation/04-Real-time-Events-API.md.

—

## 2) Accept an Order (set ETA)

### POST /api/v1/orders/{orderId}/accept

- Authorization: MustBeRestaurantStaff.
- Request body
```json
{ "restaurantId": "b1c2...", "estimatedDeliveryTime": "2025-09-14T11:00:00Z" }
```
- Preconditions: Order status is `Placed`.
- Response 200 (OrderLifecycleResultDto)
```json
{
  "orderId": "f47ac10b-...",
  "orderNumber": "ORD-20250914-102210-1234",
  "status": "Accepted",
  "placementTimestamp": "2025-09-14T10:20:00Z",
  "lastUpdateTimestamp": "2025-09-14T10:22:10Z",
  "estimatedDeliveryTime": "2025-09-14T11:00:00Z",
  "actualDeliveryTime": null
}
```
- Errors: 404 `AcceptOrder.NotFound`; 400 `Order.InvalidOrderStatusForAccept`; 403 restaurant mismatch.

—

## 3) Kitchen Progression

### POST /api/v1/orders/{orderId}/preparing
- Body: `{ "restaurantId": "b1c2..." }`
- Preconditions: status `Accepted`.
- Response 200: OrderLifecycleResultDto.
- Errors: 404 `MarkOrderPreparing.NotFound`; 400 `Order.InvalidOrderStatusForPreparing`; 403 restaurant mismatch.

### POST /api/v1/orders/{orderId}/ready
- Body: `{ "restaurantId": "b1c2..." }`
- Preconditions: status `Preparing`.
- Response 200: OrderLifecycleResultDto.
- Errors: 404 `MarkOrderReadyForDelivery.NotFound`; 400 `Order.InvalidOrderStatusForReadyForDelivery`; 403 restaurant mismatch.

### POST /api/v1/orders/{orderId}/delivered
- Body: `{ "restaurantId": "b1c2...", "deliveredAtUtc": "2025-09-14T11:25:10Z" }` (timestamp optional)
- Preconditions: status `ReadyForDelivery`.
- Side effect: sets `actualDeliveryTime`.
- Response 200: OrderLifecycleResultDto.
- Errors: 404 `MarkOrderDelivered.NotFound`; 400 `Order.InvalidOrderStatusForDelivered`; 403 restaurant mismatch.

Idempotency
- If an action is applied to an order already in the target state, the API returns the current lifecycle DTO (200) without error.

—

## 4) Exceptions

### Reject (when you cannot fulfill)

POST /api/v1/orders/{orderId}/reject
- Body: `{ "restaurantId": "b1c2...", "reason": "Kitchen at capacity" }`
- Preconditions: status `Placed`.
- Response 200: OrderLifecycleResultDto.
- Errors: 404 `RejectOrder.NotFound`; 400 `Order.InvalidStatusForReject`; 403 restaurant mismatch.

### Cancel (various reasons)

POST /api/v1/orders/{orderId}/cancel
- Body: `{ "restaurantId": "b1c2...", "actingUserId": "(optional)", "reason": "Customer requested" }`
- Preconditions by actor:
  - Staff/Admin: status in {`Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`}.
  - Customer actor: only from `Placed` or `Accepted` (`CancelOrder.InvalidStatusForCustomer` otherwise).
- Response 200: OrderLifecycleResultDto.
- Errors: 404 `CancelOrder.NotFound`; 400 `CancelOrder.InvalidStatusForCustomer` (when actor is customer); 403 insufficient permission or restaurant mismatch.

—

## 5) Payments & Webhooks (context)

- Payment success webhook moves orders from `AwaitingPayment` → `Placed` and emits lifecycle/payment events.
- Payment failure webhook moves orders to `Cancelled`.
- Provider UIs should subscribe to order events so new `Placed` orders appear in real time.

—

## Real-time & Notifications

- On every lifecycle change (Accept/Preparing/Ready/Delivered) and on payment events, the platform can push updates to provider clients; see the Real-time Events API for event names and payloads.
- Use real-time to drive kitchen screens while falling back to queue polling.

—

## Error Codes (selected)

- `Order.InvalidOrderStatusForAccept`
- `Order.InvalidStatusForReject`
- `Order.InvalidOrderStatusForPreparing`
- `Order.InvalidOrderStatusForReadyForDelivery`
- `Order.InvalidOrderStatusForDelivered`
- `CancelOrder.InvalidStatusForCustomer`

Standard responses:
- Reads: 200 OK with body
- Mutations: 200 OK with lifecycle DTO (or 204 for simple updates where applicable)
- Errors: 400/401/403/404/409 as per endpoint semantics

Versioning: All routes use `/api/v1/`.
