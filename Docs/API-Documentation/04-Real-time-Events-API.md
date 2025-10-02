## Real-time Events API

The YummyZoom platform provides a real-time API powered by SignalR for instant updates on orders and TeamCart collaboration. This enables clients to react to changes without polling.

### Connecting to Hubs

- **Hub Endpoints:**
  - Customer Orders: `/hubs/customer-orders`
  - Restaurant Orders: `/hubs/restaurant-orders`
  - TeamCart: `/hubs/teamcart` (feature-flagged; may be unavailable)
- **Authentication:** All hubs require JWT bearer authentication. Include the access token in the negotiation request.

Example (JavaScript):

```javascript
import * as signalR from '@microsoft/signalr';

const token = '<YOUR_JWT_TOKEN>';

const customerConnection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.yummyzoom.com/hubs/customer-orders', {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

await customerConnection.start();
```

---

### Customer Orders Hub (`/hubs/customer-orders`)

- **Audience:** End-user customers tracking individual orders
- **Authorization:** Authenticated user; subscription is resource-checked per order (policy `MustBeOrderOwner`).

#### Client-to-Server Methods

- `SubscribeToOrder(orderId: UUID)`
  - Subscribes the current connection to updates for a single order.
  - Authorization: caller must own the order.
- `UnsubscribeFromOrder(orderId: UUID)`
  - Removes the current connection from that order's group.

#### Server-to-Client Events

All events for this hub use the same payload shape `OrderStatusBroadcastDto`.

- `ReceiveOrderPlaced(payload)`
- `ReceiveOrderPaymentSucceeded(payload)`
- `ReceiveOrderPaymentFailed(payload)`
- `ReceiveOrderStatusChanged(payload)`

##### Payload: `OrderStatusBroadcastDto`

```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "orderNumber": "YZ-1024",
  "status": "Preparing",
  "restaurantId": "f3b63b0c-3b2a-4f8c-9d6a-3a9d1a2a4b0e",
  "lastUpdateTimestamp": "2025-01-20T10:05:00Z",
  "estimatedDeliveryTime": "2025-01-20T10:35:00Z",
  "actualDeliveryTime": null,
  "deliveredAt": null,
  "occurredOnUtc": "2025-01-20T10:05:00Z",
  "eventId": "2b3f90a2-2c1c-4a64-a20f-2a2d0cdbb4f1"
}
```

Field descriptions:

- `orderId` (UUID): Target order identifier
- `orderNumber` (string): Human-readable order number
- `status` (string): Current order status
- `restaurantId` (UUID): Restaurant to which the order belongs
- `lastUpdateTimestamp` (string, ISO 8601): Last update on the order
- `estimatedDeliveryTime` (string, ISO 8601, nullable): Current ETA
- `actualDeliveryTime` (string, ISO 8601, nullable): Actual delivery time if tracked
- `deliveredAt` (string, ISO 8601, nullable): Time the order was marked delivered
- `occurredOnUtc` (string, ISO 8601): Time the triggering event occurred
- `eventId` (UUID): Unique identifier for the broadcast event

---

### Restaurant Orders Hub (`/hubs/restaurant-orders`)

- **Audience:** Restaurant staff and owners monitoring orders for their restaurant
- **Authorization:** Authenticated user; subscription checked per restaurant (policies `MustBeRestaurantOwner` or `MustBeRestaurantStaff`).

#### Client-to-Server Methods

- `SubscribeToRestaurant(restaurantId: UUID)`
  - Adds the connection to the group for that restaurant.
- `UnsubscribeFromRestaurant(restaurantId: UUID)`
  - Removes the connection from the restaurant group.

#### Server-to-Client Events

Restaurant clients receive the same event names and payload schema as the Customer hub:

- `ReceiveOrderPlaced(payload: OrderStatusBroadcastDto)`
- `ReceiveOrderPaymentSucceeded(payload: OrderStatusBroadcastDto)`
- `ReceiveOrderPaymentFailed(payload: OrderStatusBroadcastDto)`
- `ReceiveOrderStatusChanged(payload: OrderStatusBroadcastDto)`

---

### TeamCart Hub (`/hubs/teamcart`)

- **Audience:** TeamCart participants (host and guests) collaborating on a shared cart
- **Availability:** Feature-flagged; may not be mapped in all environments.
- **Authorization:** Authenticated user; membership enforced via policy `MustBeTeamCartMember`.

#### Client-to-Server Methods

- `SubscribeToCart(teamCartId: UUID)`
- `UnsubscribeFromCart(teamCartId: UUID)`

#### Server-to-Client Events

Event names published to TeamCart subscribers:

- `ReceiveCartUpdated()`
- `ReceiveLocked()`
- `ReceivePaymentEvent(userId: UUID, status: string)`
- `ReceiveReadyToConfirm()`
- `ReceiveConverted(orderId: UUID)`
- `ReceiveExpired()`

Note: These events currently send positional arguments; future revisions may unify payloads into structured JSON objects.

---

### Groups and Targeting

- Customer hub groups: `order:{orderId}`
- Restaurant hub groups: `restaurant:{restaurantId}`
- TeamCart hub groups: `teamcart:{teamCartId}`

Events are broadcast by the server to the appropriate group(s). Some events may target Restaurant, Customer, or Both audiences.

---

### Error Handling & Codes

- If a client calls a subscribe method without authorization, the hub throws a `HubException` with message `"Unauthorized"` or `"Forbidden"`.
- Connection errors follow SignalR client library semantics. Reconnect behavior can be enabled via `withAutomaticReconnect()`.

---

### Example: Customer order tracking (JavaScript)

```javascript
import * as signalR from '@microsoft/signalr';

const token = '<YOUR_JWT_TOKEN>';
const orderId = 'f47ac10b-58cc-4372-a567-0e02b2c3d479';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.yummyzoom.com/hubs/customer-orders', {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build();

connection.on('ReceiveOrderStatusChanged', (payload) => {
  console.log('Order status changed:', payload.status);
});

await connection.start();
await connection.invoke('SubscribeToOrder', orderId);
```

---

### Versioning

- Real-time events share the same API versioning policy as REST: the current hub endpoints are stable within v1 of the platform.
- Payloads like `OrderStatusBroadcastDto` are designed to be additive. New optional fields may appear; clients should ignore unknown fields.


