# CustomerOrdersHub — Design & Integration Plan

Purpose: refine the SignalR design and integration points for a `CustomerOrdersHub` based on the existing `RestaurantOrdersHub`, `IOrderRealtimeNotifier` and current event handlers. This doc is implementation-ready and lists concrete files to change, contract shapes, edge cases, tests, and a suggested sequence of small PRs.

## Task checklist
- [x] Analyze repository surface for notifier, event handlers, SignalR wiring and policies. (done)
- [x] Produce a refined design for `CustomerOrdersHub` that mirrors `RestaurantOrdersHub` patterns. (done)
- [x] Define notifier API changes / options to support customer notifications. (done)
- [x] Enumerate concrete integration points and files to edit. (done)
- [x] Provide sequence of low-risk PRs and tests. (done)


## Quick summary of current state (key findings)
- `RestaurantOrdersHub` exists and uses `IAuthorizationService` + resource-based checks (`Policies.MustBeRestaurantOwner`/`Staff`) to add connections to `restaurant:{restaurantId}` groups.
- `IOrderRealtimeNotifier` is defined and currently includes `NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto)` at minimum; a `SignalROrderRealtimeNotifier` implements the interface and broadcasts to hub groups.
- `SignalROrderRealtimeNotifier` is registered in DI (`Web/DependencyInjection.cs`) and used by many order-related domain event handlers (e.g., `OrderPaymentSucceededEventHandler`) via the application layer.
- SignalR is already enabled (`builder.Services.AddSignalR()`), and `RestaurantOrdersHub` is mapped and operational.
- Event handlers for many lifecycle events call the notifier (tests mock `IOrderRealtimeNotifier` expecting `NotifyOrderPaymentSucceeded` calls). There are existing DTOs such as `OrderStatusBroadcastDto` used for messages.

Assumption: `OrderStatusBroadcastDto` is suitable to carry minimal order status info to customers; if not, extend it as a small step.


## Design goals for `CustomerOrdersHub`
- Allow authenticated customers to subscribe to order-specific updates securely.
- Keep the same resource-authority model used by `RestaurantOrdersHub` (authorize before joining group).
- Use per-order groups (`order:{orderId}`) as primary channel; optionally support per-customer groups (`customer:{customerId}`) for aggregated messages.
- Rely on the existing `IOrderRealtimeNotifier` abstraction (extend if necessary) so application event handlers need minimal changes.


## Hub responsibilities & API
- File: `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`.

Public hub methods (server-callable from client):
- `Task SubscribeToOrder(Guid orderId)`
  - Verify `Context.User` identity and ownership of `orderId` (either via `Policies.MustBeOrderOwner` or a scoped resource authorization check).
  - Add connection to `order:{orderId}` group.
  - Return small ack or throw `HubException` on forbidden.
- `Task UnsubscribeFromOrder(Guid orderId)`
  - Remove from group and log.
- (Optional) `Task SubscribeToCustomer(Guid customerId)` / `UnsubscribeToCustomer` — requires customer-scoped auth and should be opt-in.

Group naming:
- `order:{orderId}` — recommended.
- `customer:{customerId}` — optional.

Authorization approach:
- Prefer `IAuthorizationService.AuthorizeAsync(user, new OrderResource(orderId), Policies.MustBeOrderOwner)` if `MustBeOrderOwner` policy exists and can check ownership claims.
- If `MustBeOrderOwner` is not wired in DI, fallback to an in-hub check: resolve `IOrderRepository` (or a small `IOrderReadOnlyQuery`) and confirm `Order.CustomerId == Context.User.GetCustomerId()`.


## Notifier abstraction - options
Chosen approach — minimal, explicit routing via NotificationTarget (recommended)

To meet the requirement of small changes while enabling fine-grained control, this plan adopts a lightweight routing enum called `NotificationTarget` and an optional parameter on existing notifier methods. This keeps existing method names and defaults, preserves backwards compatibility, and gives handlers explicit control to target Restaurant, Customer, or Both.

Why this approach:
- Minimal API surface change: existing call sites remain valid because the new parameter has a default value that preserves current Restaurant-only behavior.
- Fine-grained control: handlers can request `Customer`, `Restaurant`, or `Both` per-call without adding many new interface methods.
- Single implementation point: the existing `SignalROrderRealtimeNotifier` can accept an additional `IHubContext<CustomerOrdersHub>` and route messages appropriately.

Key decision details:
- Add enum `NotificationTarget { Restaurant = 1, Customer = 2, Both = Restaurant | Customer }`.
- Add an optional `NotificationTarget target = NotificationTarget.Restaurant` parameter to existing `IOrderRealtimeNotifier` methods (e.g. `NotifyOrderStatusChanged`, `NotifyOrderPaymentSucceeded`).
- Update `SignalROrderRealtimeNotifier` to accept `IHubContext<CustomerOrdersHub>` and send to the `order:{orderId}` customer group when `Customer` or `Both` is requested. Use `Task.WhenAll` when sending to multiple groups.

This preserves existing tests and behavior while enabling gradual migration of event handlers to notify customers where appropriate.


## DTOs & Contracts
- Use existing `OrderStatusBroadcastDto` when possible; fields required by customers:
  - `OrderId` (Guid)
  - `CustomerId` (Guid) (optional but useful to route)
  - `RestaurantId` (Guid)
  - `Status` (enum/string)
  - `TotalAmount` (monetary summary optional for customers)
  - `Timestamps` (Placed, Accepted, Preparing, etc.)

Contract: notifier methods accept `OrderStatusBroadcastDto` and deliver to `order:{OrderId}` group; `SignalR` client event names:
- `ReceiveOrderStatusChanged`
- `ReceiveOrderPaymentSucceeded` (existing)
- `ReceiveOrderPaymentFailed` / `ReceiveOrderCancelled` etc.


## Event handler integration points
Files likely to change:
- `src/Application/Orders/EventHandlers/OrderPaymentSucceededEventHandler.cs` (already calls notifier)
- Similar handlers: `OrderPlacedEventHandler`, `OrderAcceptedEventHandler`, `OrderPreparingEventHandler`, `OrderReadyForDeliveryEventHandler`, `OrderDeliveredEventHandler`, `OrderCancelledEventHandler`, `OrderRejectedEventHandler`

Action plan for each handler:
- Ensure the handler builds an `OrderStatusBroadcastDto` containing the `CustomerId` or minimal info needed to route.
- Call the new notifier customer method, e.g. `_notifier.NotifyOrderStatusChangedToCustomer(dto, ct);`
- Maintain idempotency guards already in the handlers (many inherit from `IdempotentNotificationHandler<T>`), so notifications are not duplicated on retries.


## DI and routing
- DI: `src/Web/DependencyInjection.cs` already registers `SignalROrderRealtimeNotifier` as `IOrderRealtimeNotifier`.
  - Update implementation to include customer methods (Option A) and keep registration unchanged.
- Map new hub path in the web host mapping (likely in `Program.cs` or a centralized routing file):
  - Map endpoint: `/hubs/customer-orders`.
  - Ensure CORS and auth are consistent with `RestaurantOrdersHub`.


## Authorization policy notes
- `Policies.MustBeOrderOwner` exists in docs but may not be wired in DI. Two options:
  1. Wire a `MustBeOrderOwner` policy in `Infrastructure/DependencyInjection.cs` using a custom requirement/handler which checks a combination of claims or a database lookup.
  2. If policy wiring is not desired, perform an explicit ownership check inside `CustomerOrdersHub.SubscribeToOrder` using a scoped read-only query.

Recommendation: start with scoped read-only check in the hub (simple, low-risk). Later extract a reusable `MustBeOrderOwner` policy.


## Tests to add
- Unit tests for `CustomerOrdersHub`:
  - `SubscribeToOrder` success when owner
  - `SubscribeToOrder` forbidden when not owner
  - `UnsubscribeFromOrder` removes group membership
- Integration / functional tests:
  - `EventHandler -> Notifier -> Hub` flow: use mocked notifier or in-memory hub to verify that `OrderPaymentSucceeded` results in a client call to `ReceiveOrderPaymentSucceeded` on `order:{orderId}` group.
  - End-to-end: client connects to hub, subscribes, and receives a broadcast when an event is processed (can be an in-memory WebApplicationFactory test).

Existing tests already mock `IOrderRealtimeNotifier` in many event handler tests; extend them to verify customer-method calls are made when appropriate.


## Observability
- Log subscription attempts (success/forbidden) with `ConnectionId`, `OrderId`, `UserId`.
- Log broadcasts to customer groups at `Debug` or `Information` level.
- Add metrics: `customer_hub_subscriptions_total`, `customer_hub_broadcasts_total`, `customer_hub_forbidden_subscriptions_total`.


## Edge cases and robustness
- Ownership race: if a user subscribes immediately after order creation but before DB transaction commits, allow a small retry/backoff or return a clear `NotFound` vs `Forbidden` error.
- Multiple devices: group model supports multiple connections per user; ensure clients remove stale connections on disconnect.
- Idempotent broadcasts: event handlers already use idempotent pattern; ensure notifier does not re-broadcast on duplicate events.
- Sensitive data: do not broadcast fields customers shouldn't see (internal fees, other customers' personal data).


## Files to create / update (concrete list)
- Add: `src/Web/Realtime/Hubs/CustomerOrdersHub.cs` — hub implementation (mirror `RestaurantOrdersHub`).
- Update: `src/Application/Common/Interfaces/IServices/IOrderRealtimeNotifier.cs` — add customer methods (Option A) and keep old signatures (backwards compatible).
- Update: `src/Web/Realtime/SignalROrderRealtimeNotifier.cs` — implement new methods and route to `order:{orderId}` and `customer:{customerId}` groups.
- Update: `src/Web/DependencyInjection.cs` — ensure notifier registration remains valid and map hub endpoint `/hubs/customer-orders` (if mapping centralized here or `Program.cs`).
- Update event handlers: `src/Application/Orders/EventHandlers/*EventHandler.cs` to call notifier customer methods where appropriate.
- Add tests: `tests/Application.FunctionalTests/Web/SignalR/CustomerOrdersHubTests.cs` and update relevant event handler tests to expect customer notifier calls.


## Suggested incremental PR sequence
1. Create `CustomerOrdersHub` + simple ownership check using `IOrderReadOnlyQuery` (no notifier changes). Add unit tests for hub subscription logic.
2. Extend `IOrderRealtimeNotifier` with customer methods and update `SignalROrderRealtimeNotifier` to implement them. Add tests for notifier.
3. Update event handlers to call customer notifier methods and extend existing event handler tests to cover customer notifications.
4. Map `/hubs/customer-orders` and add minimal client docs in `Docs/` (usage snippet).
5. Optional: extract `MustBeOrderOwner` policy and wire into DI; replace hub's manual check with policy.

## Step-by-step implementation (detailed)

Follow these concrete, small PRs. Each PR is minimal and verifiable.

### PR 1 — Hub skeleton and ownership check
- Add file: `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`
  - Implement a hub modeled after `RestaurantOrdersHub` exposing `SubscribeToOrder(Guid orderId)` and `UnsubscribeFromOrder(Guid orderId)`.
  - Authorization: prefer `IAuthorizationService.AuthorizeAsync(Context.User, new OrderResource(orderId), Policies.MustBeOrderOwner)` if the policy exists; otherwise perform a scoped read-only lookup using `IOrderReadOnlyQuery` or `IOrderRepository` to confirm `Order.CustomerId == currentUserId`.
  - Use group name: `order:{orderId}`.
  - Log subscription/unsubscription attempts including `ConnectionId`, `OrderId`, and `UserId`.

Tests for PR1
- Unit tests: `tests/Application.FunctionalTests/Web/SignalR/CustomerOrdersHubTests.cs` with cases:
  - Subscribe success when user owns order.
  - Subscribe forbidden when user does not own order.
  - Unsubscribe removes connection from group.

Quick local test command for PR1
```pwsh
# Run only the functional tests (adjust filter as needed)
dotnet test tests\Application.FunctionalTests\ --filter FullyQualifiedName~CustomerOrdersHubTests
```

### PR 2 — Add NotificationTarget enum and optional parameter
- Add file: `src/Application/Common/Interfaces/IServices/NotificationTarget.cs`
  - Content: `public enum NotificationTarget { Restaurant = 1, Customer = 2, Both = Restaurant | Customer }`.
- Update interface: `src/Application/Common/Interfaces/IServices/IOrderRealtimeNotifier.cs`
  - Add optional parameter `NotificationTarget target = NotificationTarget.Restaurant` to existing notify methods (e.g., `Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken ct = default);`).
  - Keep signatures otherwise unchanged so existing callers are unaffected.

Tests for PR2
- Update mocks in tests that setup or verify `IOrderRealtimeNotifier` to accept the new optional argument (use `It.IsAny<NotificationTarget>()` where necessary).

### PR 3 — Wire SignalROrderRealtimeNotifier to support customer hub
- Update file: `src/Web/Realtime/SignalROrderRealtimeNotifier.cs`
  - Add constructor param: `IHubContext<CustomerOrdersHub> customerHubContext` and store it.
  - Compute groups per call:
    - `restaurantGroup = $"restaurant:{dto.RestaurantId}"`
    - `orderGroup = $"order:{dto.OrderId}"`
  - Branch on `target` and send appropriately:
    - Restaurant: send to `_hubContext.Clients.Group(restaurantGroup)`
    - Customer: send to `_customerHubContext.Clients.Group(orderGroup)`
    - Both: `await Task.WhenAll(restaurantSend, customerSend)`
  - Wrap per-target sends in try/catch and log errors; do not throw to avoid breaking event handling.

DI notes for PR3
- No special DI registration changes are required; ASP.NET Core will resolve `IHubContext<CustomerOrdersHub>` as long as `CustomerOrdersHub` exists and SignalR is registered.

Tests for PR3
- Update `tests/Application.FunctionalTests/Web/SignalR/SignalROrderRealtimeNotifierTests.cs` to mock both `IHubContext<RestaurantOrdersHub>` and `IHubContext<CustomerOrdersHub>` and verify SendAsync is called for selected `NotificationTarget` values.

### PR 4 — Update event handlers to target customers where appropriate
- Update handlers (examples):
  - `src/Application/Orders/EventHandlers/OrderPaymentSucceededEventHandler.cs` — call `_notifier.NotifyOrderPaymentSucceeded(dto, NotificationTarget.Both, ct);` or `Customer` based on business rule.
  - `OrderPlacedEventHandler`, `OrderAcceptedEventHandler`, `OrderPreparingEventHandler`, `OrderReadyForDeliveryEventHandler`, `OrderDeliveredEventHandler`, etc.
  - Ensure each handler includes `OrderId` (and `CustomerId` if available) in `OrderStatusBroadcastDto`.

Tests for PR4
- Extend existing handler tests that mock `IOrderRealtimeNotifier` and verify calls include the `NotificationTarget` argument where relevant.

### PR 5 — Map hub endpoint & docs
- Map endpoint: `/hubs/customer-orders` in the web app routing (`Program.cs` or central endpoints mapping).
- Add quick client snippet to `Docs/` showing how to connect, authenticate, call `SubscribeToOrder`, and handle `ReceiveOrderStatusChanged`.

### PR 6 — Optional: extract `MustBeOrderOwner` policy
- Add requirement and handler under `src/Infrastructure/Authorization/` and register in `src/Infrastructure/DependencyInjection.cs`.
- Replace hub's manual ownership lookup with `IAuthorizationService.AuthorizeAsync(Context.User, new OrderResource(orderId), Policies.MustBeOrderOwner)`.

### Verification & quality gates
- After each PR run:
```pwsh
dotnet build YummyZoom.sln
dotnet test tests\\Application.FunctionalTests\\ --no-build
```

Rollout notes
- Merge PRs in order: PR1, PR2, PR3, PR4, PR5. PR2 can be merged before PR3 to reduce churn since it is additive and uses default values.
- Because the `NotificationTarget` parameter defaults to `Restaurant`, existing behavior is preserved until handlers are updated.


## Quality gates
- Build and run existing SignalR unit tests (many already exist). Ensure no breaking changes to mocked notifier uses.
- Add the new hub unit tests and run them.
- Run a small functional test where the notifier is replaced with real `SignalROrderRealtimeNotifier` and a test client receives messages (optional smoke test).


## Small follow-ups (Phase 1.1)
- Add `CustomerOrdersHub` support for anonymous guest order tracking (public token-based group join).
- Add customer presence / typing / delivery ETA updates.
- Add platform fee visibility toggles for customers.


## Acceptance criteria
- Customers can open a SignalR connection and subscribe to `order:{orderId}` only when they own the order.
- Relevant lifecycle events broadcast to the `order:{orderId}` group and are received by subscribed customer clients.
- Event handlers remain idempotent and tests cover duplicate webhook/event delivery scenarios.


---

Notes / assumptions:
- This doc assumes `OrderStatusBroadcastDto` exists and is reused. If it's missing fields, add minimal extensions.
- If `MustBeOrderOwner` policy is already implemented in code, the hub should use `IAuthorizationService` similarly to `RestaurantOrdersHub`.

End of design doc.
