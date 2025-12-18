# Real-time Hubs (Restaurant FE Integration)

This document specifies the SignalR hub(s) that restaurant frontends use to receive real-time order updates.

Cross‑refs:
- Platform overview: `Docs/API-Documentation/04-Real-time-Events-API.md`
- Restaurant workflow guide: `Docs/API-Documentation/API-Reference/Restaurant/Workflows/04-Real-time-Operations.md`

---

## Restaurant Orders Hub

Hub path: `/hubs/restaurant-orders`  
Audience: restaurant staff dashboards, kitchen screens, and owner/admin UIs.

### Authentication

- Required: JWT bearer authentication (`Authorization: Bearer <token>`).
- SignalR JS clients typically pass the token via `accessTokenFactory`.

### Authorization model (important)

Restaurant clients do **not** receive events until they explicitly subscribe to a restaurant group.

- Server-side authorization is enforced on subscription:
  - Allowed if the authenticated user satisfies `MustBeRestaurantOwner` or `MustBeRestaurantStaff` for the target restaurant.
- Group name format:
  - `restaurant:{restaurantId}`

### Client → Server methods

#### `SubscribeToRestaurant(restaurantId: UUID)`

Adds the current connection to the restaurant’s SignalR group.

- On success: connection begins receiving order events for that restaurant.
- On failure:
  - Throws `HubException("Unauthorized")` if the caller is not authenticated.
  - Throws `HubException("Forbidden")` if the caller lacks restaurant access.

#### `UnsubscribeFromRestaurant(restaurantId: UUID)`

Removes the current connection from the restaurant’s group.

### Server → Client events

All events use the same JSON payload: `OrderStatusBroadcastDto`.

Event names:
- `ReceiveOrderPlaced(payload: OrderStatusBroadcastDto)`
- `ReceiveOrderPaymentSucceeded(payload: OrderStatusBroadcastDto)`
- `ReceiveOrderStatusChanged(payload: OrderStatusBroadcastDto)`

Notes:
- `ReceiveOrderPaymentFailed` exists as a server event name, but **restaurant clients should not rely on it** in the current business flow. Payment failures transition the order to `Cancelled`, and restaurants will observe that via `ReceiveOrderStatusChanged` (status = `Cancelled`).

#### Payload: `OrderStatusBroadcastDto`

````json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "orderNumber": "ORD-20250914-102210-1234",
  "status": "Preparing",
  "restaurantId": "f3b63b0c-3b2a-4f8c-9d6a-3a9d1a2a4b0e",
  "lastUpdateTimestamp": "2025-01-20T10:05:00Z",
  "estimatedDeliveryTime": "2025-01-20T10:35:00Z",
  "actualDeliveryTime": null,
  "deliveredAt": null,
  "occurredOnUtc": "2025-01-20T10:05:00Z",
  "eventId": "2b3f90a2-2c1c-4a64-a20f-2a2d0cdbb4f1"
}
````

Semantics:
- `status` is the current order lifecycle status string (e.g., `Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`, `Delivered`, `Cancelled`, `Rejected`).
- `eventId` uniquely identifies the triggering event (use it for client-side deduplication).
- Timestamps are UTC ISO 8601.

### Event coverage (restaurant expectations)

Restaurants should expect real-time updates for:
- New actionable order arrival: `ReceiveOrderPlaced`
- Online payment completion: `ReceiveOrderPaymentSucceeded`
- Lifecycle transitions (staff + system): `ReceiveOrderStatusChanged` including statuses such as `Accepted`, `Rejected`, `Preparing`, `ReadyForDelivery`, `Delivered`, `Cancelled`

### Reliability and deduplication

Delivery is at-least-once for restaurant broadcasts:
- Server retries some failures, so duplicates can occur.
- Restaurant UIs should treat events as idempotent:
  - Deduplicate using `(orderId, eventId)` or simply `eventId` if globally unique in your event store.

Important: SignalR is not an offline queue.
- If the client was disconnected when an event occurred, it may miss it.
- On connect/reconnect, fetch current state via REST (e.g., restaurant “new” and “active” order endpoints) and use hub events only to stay up to date.

### Reconnect behavior (recommended)

Restaurant UIs should:
- Use automatic reconnect (`withAutomaticReconnect`).
- Re-subscribe after reconnect (SignalR group membership is per-connection).

Example (JavaScript)
```js
import * as signalR from '@microsoft/signalr';

const accessToken = () => /* return current JWT string */;
const restaurantId = 'f3b63b0c-3b2a-4f8c-9d6a-3a9d1a2a4b0e';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/restaurant-orders', { accessTokenFactory: accessToken })
  .withAutomaticReconnect([0, 2000, 5000, 10000])
  .build();

connection.on('ReceiveOrderPlaced', (dto) => handleOrderEvent(dto));
connection.on('ReceiveOrderPaymentSucceeded', (dto) => handleOrderEvent(dto));
connection.on('ReceiveOrderStatusChanged', (dto) => handleOrderEvent(dto));

connection.onreconnected(async () => {
  await connection.invoke('SubscribeToRestaurant', restaurantId);
});

await connection.start();
await connection.invoke('SubscribeToRestaurant', restaurantId);

function handleOrderEvent(dto) {
  // Update queue state; optionally refetch details if needed.
  console.log(dto.orderId, dto.status, dto.eventId);
}
```

### Errors

- Unauthorized subscription: `HubException("Unauthorized")`
- Forbidden subscription: `HubException("Forbidden")`

---

## Environments and scaling note

If the API is deployed with multiple web instances, SignalR requires a scale-out mechanism (e.g., Redis backplane or Azure SignalR Service) to ensure group broadcasts reach connections attached to other nodes.

