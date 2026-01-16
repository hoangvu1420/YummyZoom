# SignalR Restaurant Notifications (Order Lifecycle) — Current Implementation Review

Date: 2025-12-18  
Scope: Restaurant-facing real-time notifications for order-related actions (lifecycle + payment) implemented via SignalR.

## 1) What exists today (high-level)

The current real-time design is:

1. **Domain events** are raised on the `Order` aggregate (e.g., `OrderPlaced`, `OrderCancelled`, `OrderDelivered`).
2. **Application event handlers** (in `src/Application/Orders/EventHandlers`) react to those events and call an abstraction: `IOrderRealtimeNotifier`.
3. In the Web host, `IOrderRealtimeNotifier` is implemented by `SignalROrderRealtimeNotifier` which broadcasts over SignalR to hub **groups**:
   - Restaurant group: `restaurant:{RestaurantId}`
   - Customer group: `order:{OrderId}`

SignalR hubs are mapped at:

- `GET/WS /hubs/restaurant-orders` → `RestaurantOrdersHub`
- `GET/WS /hubs/customer-orders` → `CustomerOrdersHub`

## 2) Restaurant hub subscription and authorization

**Hub:** `src/Web/Realtime/Hubs/RestaurantOrdersHub.cs`  
**Group name:** `restaurant:{restaurantId}`  
**Subscription method:** `SubscribeToRestaurant(Guid restaurantId)`

Authorization behavior:

- The hub requires an authenticated user (`[Authorize]`).
- `SubscribeToRestaurant` performs an authorization check using `IAuthorizationService`:
  - First tries `Policies.MustBeRestaurantOwner`.
  - If not owner, tries `Policies.MustBeRestaurantStaff`.
- Only if authorized does it add the connection to the restaurant group.

This is a solid base: group membership is not purely client-controlled; it is server-authorized per restaurant.

## 3) SignalR message contract (server → client)

**Notifier:** `src/Web/Realtime/SignalROrderRealtimeNotifier.cs`  
**Payload:** `OrderStatusBroadcastDto` (`src/Application/Orders/Broadcasting/OrderStatusBroadcastDto.cs`)

Current SignalR method names used:

- `ReceiveOrderPlaced`
- `ReceiveOrderPaymentSucceeded`
- `ReceiveOrderPaymentFailed`
- `ReceiveOrderStatusChanged`

The payload includes:

- Identifiers: `OrderId`, `RestaurantId`, `EventId`
- Status snapshot: `Status`, `OrderNumber`, timestamps (`OccurredOnUtc`, `LastUpdateTimestamp`, etc.)

## 4) Implemented notification logic to the restaurant side

The restaurant audience is targeted via `NotificationTarget.Restaurant` or `NotificationTarget.Both`, which results in a group broadcast to `restaurant:{RestaurantId}`.

### 4.1 Coverage matrix (domain event → restaurant SignalR broadcast)

The following mapping reflects the code in `src/Application/Orders/EventHandlers/*.cs`:

| Domain event | Handler | Notifier call | Restaurant receives? |
|---|---|---|---|
| `OrderPlaced` | `OrderPlacedEventHandler` | `NotifyOrderPlaced(dto, Restaurant)` | Yes |
| `OrderPaymentSucceeded` | `OrderPaymentSucceededEventHandler` | `NotifyOrderPaymentSucceeded(dto, Both)` | Yes |
| `OrderPaymentFailed` | `OrderPaymentFailedEventHandler` | `NotifyOrderPaymentFailed(dto, Customer)` | No (but see note below) |
| `OrderCancelled` | `OrderCancelledEventHandler` | `NotifyOrderStatusChanged(dto, Restaurant)` | Yes |
| `OrderDelivered` | `OrderDeliveredEventHandler` | `NotifyOrderStatusChanged(dto, Both)` | Yes |
| `OrderAccepted` | `OrderAcceptedEventHandler` | `NotifyOrderStatusChanged(dto, Customer)` | No |
| `OrderRejected` | `OrderRejectedEventHandler` | `NotifyOrderStatusChanged(dto, Customer)` | No |
| `OrderPreparing` | `OrderPreparingEventHandler` | `NotifyOrderStatusChanged(dto, Customer)` | No |
| `OrderReadyForDelivery` | `OrderReadyForDeliveryEventHandler` | `NotifyOrderStatusChanged(dto, Customer)` | No |

**Important note about payment failure:** the domain raises both `OrderPaymentFailed` and `OrderCancelled` on payment failure. Even though the payment-failed handler targets customers only, the cancellation handler does notify the restaurant (as a status change to `Cancelled`). This means restaurants will observe the *resulting cancellation*, but not the *payment-failure-specific* message.

### 4.2 What restaurant dashboards can infer today

With the above, a restaurant dashboard that subscribes to a restaurant group will reliably get:

- New order arrival (`ReceiveOrderPlaced`) for COD orders and also for online-paid orders (because `RecordPaymentSuccess` raises `OrderPlaced` after `OrderPaymentSucceeded`).
- Payment succeeded (`ReceiveOrderPaymentSucceeded`) for online payment flows.
- Cancellation and delivery transitions (`ReceiveOrderStatusChanged`) for those events.

However, restaurant dashboards will **not** receive status-change broadcasts for accepted/rejected/preparing/ready-for-delivery transitions.

## 5) Sufficiency assessment (restaurant-side real-time behavior)

### 5.1 What is sufficient / good

- **Authorization on subscription is present** for restaurant groups.
- **Group-based fan-out** (`restaurant:{RestaurantId}`) matches a common dashboard model (multiple staff devices).
- **Event handlers are idempotent** via inbox (`IdempotentNotificationHandler<T>`), preventing repeated execution when retried.
- The DTO contains `EventId`/`OccurredOnUtc`, which is a good starting point for correlation and ordering.

### 5.2 Gaps / risks for restaurant notifications

1. **Restaurant does not receive several lifecycle transitions** (`Accepted`, `Rejected`, `Preparing`, `ReadyForDelivery`).
   - If you expect multiple staff devices to stay synchronized, the current approach is incomplete.
   - This also contradicts several handler doc comments (e.g., “broadcast to restaurant dashboards and customer interfaces”) where the code currently targets customers only.

2. **Transient SignalR failures are “best-effort” and not retried.**
   - `SignalROrderRealtimeNotifier` catches exceptions per target and does **not rethrow**.
   - Because handlers treat the notifier call as “successful” even on failure, the inbox record is written and the event will **not** retry.
   - This differs from `SignalRTeamCartRealtimeNotifier`, which rethrows to allow retries.

3. **Multi-instance deployment needs a backplane / managed SignalR service.**
   - `AddSignalR()` alone is fine for a single server.
   - In a scaled-out environment, group broadcasts won’t reach connections attached to other nodes unless you configure a backplane (e.g., Redis) or Azure SignalR Service.

4. **No explicit “resync” / “catch-up” contract.**
   - If a restaurant client is disconnected during an event, it will miss the message.
   - This is normally mitigated by fetching current state over REST on (re)connect, but that expectation should be made explicit in the client contract, and the DTO should help the client reason about staleness.

## 6) Suggested enhancements (recommended roadmap)

### 6.1 Correctness / consistency (high priority)

- Decide the intended audience per lifecycle event, then **align code + comments**:
  - If restaurant dashboards should reflect all lifecycle changes (recommended for multi-device staff), update the following handlers to target `NotificationTarget.Both` (or at least include `Restaurant`):
    - `OrderAcceptedEventHandler`
    - `OrderRejectedEventHandler`
    - `OrderPreparingEventHandler`
    - `OrderReadyForDeliveryEventHandler`
  - If the intent is “restaurant only needs Placed/Cancelled/Delivered”, then update the XML docs/comments to match reality.

- Make broadcast reliability an explicit decision:
  - If “at least once” is desired, change `SignalROrderRealtimeNotifier` to **rethrow** on failures (or provide a policy knob per target), so the idempotent inbox can drive retries.
  - If “best-effort” is acceptable, consider persisting failed sends (outbox) or at least emitting metrics so you can detect loss.

### 6.2 Scale-out readiness (high priority for production)

- Configure a SignalR scale-out mechanism for production:
  - Azure SignalR Service (common in Azure-hosted apps), or
  - Redis backplane for SignalR.

Without this, restaurant staff connected to different app instances will experience missing updates.

### 6.3 Client contract hardening (medium priority)

- Add `OrderVersion` to `OrderStatusBroadcastDto` (the domain already increments `Order.Version`).
  - This enables clients to ignore stale events and to detect gaps (“I saw version 12, then I received version 14”).

- Consider unifying server method names to a single event (e.g., `ReceiveOrderUpdated`) with an `EventType` field, to simplify client wiring and reduce surface area.

- Centralize SignalR method-name constants (or use strongly typed hubs) to avoid string drift between server and client.

### 6.4 Restaurant offline notifications (optional / product-dependent)

- Restaurants currently rely on being connected to SignalR; there is no restaurant push-notification equivalent to customer FCM.
- If the product needs “wake up / new order alert” when a tablet is locked or the dashboard is closed, introduce a restaurant push channel:
  - FCM/APNs to staff devices, or
  - SMS/WhatsApp/email for escalation depending on SLA requirements.

## 7) Quick pointers to the implementation

- Hubs:
  - `src/Web/Realtime/Hubs/RestaurantOrdersHub.cs`
  - `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`
- Notifier implementation:
  - `src/Web/Realtime/SignalROrderRealtimeNotifier.cs`
- Event handlers (where “what gets notified” is decided):
  - `src/Application/Orders/EventHandlers/*.cs`
- Hub mapping and service registration:
  - `src/Web/Program.cs`
  - `src/Web/DependencyInjection.cs`

