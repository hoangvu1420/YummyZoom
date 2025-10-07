# Workflow: Real-time Operations (Provider)

This guide shows how restaurant UIs consume live order updates using SignalR hubs. It covers connecting, authenticating, subscribing to a restaurant, handling events, and operational best practices.

—

## Overview

- Hubs (SignalR)
  - Provider: `/hubs/restaurant-orders`
  - Customer: `/hubs/customer-orders` (for reference)
  - TeamCart (feature-flagged): `/hubs/teamcart`
- Auth: Hubs require an authenticated JWT; send the bearer token during negotiation.
- Subscription: Providers subscribe per-restaurant; customers subscribe per-order; TeamCart subscribers join per-cart.
- Events (provider-facing):
  - `ReceiveOrderPlaced`
  - `ReceiveOrderPaymentSucceeded`
  - `ReceiveOrderPaymentFailed`
  - `ReceiveOrderStatusChanged`
- Payload: `OrderStatusBroadcastDto` (see below).

—

## Connect & Authenticate

Example (JavaScript)
```js
import * as signalR from '@microsoft/signalr';

const accessToken = () => /* return current JWT string */;

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/restaurant-orders', {
    accessTokenFactory: accessToken
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000])
  .build();

await connection.start();
```

Notes
- Use your backend’s base URL (e.g., `https://api.yourdomain.com/hubs/restaurant-orders`).
- Automatic reconnect is recommended; re-subscribe to groups on reconnect.

—

## Subscribe to a Restaurant

After establishing the connection, authorize and join the restaurant group:

```js
await connection.invoke('SubscribeToRestaurant', restaurantId);

// (optional later)
await connection.invoke('UnsubscribeFromRestaurant', restaurantId);
```

Rules
- Caller must satisfy provider policy for that restaurant (`MustBeRestaurantOwner` or `MustBeRestaurantStaff`).
- On failure, the hub throws a `HubException` with message `Unauthorized` or `Forbidden`.

—

## Handle Events

Register handlers before `connection.start()` or immediately after, then (re)register after reconnect.

```js
connection.on('ReceiveOrderPlaced', onOrderEvent);
connection.on('ReceiveOrderPaymentSucceeded', onOrderEvent);
connection.on('ReceiveOrderPaymentFailed', onOrderEvent);
connection.on('ReceiveOrderStatusChanged', onOrderEvent);

function onOrderEvent(dto) {
  // dto is OrderStatusBroadcastDto
  // Update kitchen screens, sound alerts, etc.
}
```

OrderStatusBroadcastDto
```json
{
  "orderId": "f47ac10b-...",
  "orderNumber": "ORD-20250914-102210-1234",
  "status": "Accepted",
  "restaurantId": "a1b2c3d4-...",
  "lastUpdateTimestamp": "2025-09-14T10:22:10Z",
  "estimatedDeliveryTime": "2025-09-14T11:00:00Z",
  "actualDeliveryTime": null,
  "deliveredAt": null,
  "occurredOnUtc": "2025-09-14T10:22:10Z",
  "eventId": "2c6f5e48-..."
}
```

Event semantics
- Placed/payment events are emitted when orders are placed or payment succeeds/fails.
- Status-changed covers transitions (Accepted, Preparing, ReadyForDelivery, Delivered).

—

## TeamCart Real-time (optional)

If TeamCart is enabled, collaborative sessions use `/hubs/teamcart`.

Subscribe
```js
await connectionToTeamCart.invoke('SubscribeToCart', teamCartId);
```

Events (typical)
- Membership changes, item updates, quote updates, lock for payment, payment events, conversion.
- Provider flow impact: once converted, the created Order surfaces in provider order streams and real-time events above.

—

## Best Practices

- Re-subscribe after reconnect: keep an in-memory list of subscribed restaurant IDs and rejoin groups in the `onreconnected` callback.
- Idempotent UI updates: use `eventId` to ignore duplicate events on retries.
- Pair real-time with queue APIs:
  - Reads: `GET /restaurants/{id}/orders/new` and `/active` for initial state.
  - Live updates: handle hub events to keep screens current.
- Alerting: play audible cues on `ReceiveOrderPlaced` to assist intake.

—

## Errors & Security

- Hubs require authentication; include the bearer token via `accessTokenFactory`.
- Authorization failures throw `HubException` with `Forbidden`.
- Do not expose JWTs in logs; rotate tokens and handle 401/403 by forcing re-auth.

Versioning: Hubs are unversioned paths (`/hubs/...`); related REST routes are under `/api/v1/`.