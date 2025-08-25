# Task 3.6  SignalR Real-time Layer  Detailed Plan

Purpose: Design and wire a production-ready real-time channel for the restaurant dashboard to receive order lifecycle/payment updates, replacing the current no-op notifier with a SignalR-backed implementation. This plan aligns with existing Clean Architecture, Outbox/Inbox, and MediatR patterns in the repo.

---

## Current State (as implemented)

- Architectural patterns
  - Clean Architecture solution (Domain, Application, Infrastructure, Web), MediatR, minimal APIs, EF Core.
  - Outbox + Hosted outbox processor publishing domain events via MediatR asynchronously.
  - Inbox + IdempotentNotificationHandler<TEvent> base ensures exactly-once per handler.
  - Strongly-typed IDs and System.Text.Json converters wired across layers.

- Domain & Application
  - Order aggregate emits lifecycle and payment events.
  - Event handlers exist for OrderAccepted/Rejected/Cancelled/Preparing/ReadyForDelivery/Delivered and OrderPaymentSucceeded/Failed.
  - Broadcasting contract: `OrderStatusBroadcastDto` (order and lifecycle snapshot).
  - Abstraction for real-time: `IOrderRealtimeNotifier`.
  - Handlers call the notifier and are idempotent and retried by Outbox processor on failure.

- Infrastructure
  - `NoOpOrderRealtimeNotifier` is registered (singleton) as the current implementation.
  - Outbox/Inbox repositories, EF config, hosted service, and processor exist and are used by functional tests.

- Web
  - Minimal APIs, versioning, NSwag, exception pipeline are configured.
  - No SignalR setup yet (no hubs, no registration, no endpoints).

Implication: The pipeline from domain events to a real-time abstraction is already done; only the transport needs to be implemented and registered.

---

## Goals for Task 3.6

- Add SignalR to Web, expose a hub for restaurant staff dashboards.
- Replace the no-op notifier with a SignalR-backed implementation (registered in Web to avoid Infrastructure->Web coupling).
- Group-based delivery: restaurant staff receive only their restaurant's updates.
- Keep handlers and DTO contracts unchanged; maintain idempotency and retry behavior via Outbox/Inbox.
- Prepare for scale-out and future customer-facing hub without blocking Phase 1.

---

## Proposed Design

- Hubs
  - RestaurantOrdersHub (Phase 1): server-authorized group delivery per restaurant.
  - Future: CustomerOrderStatusHub (order/customer groups)  explicitly deferred.

- Grouping model
  - Group name: `restaurant:{RestaurantId}` for all restaurant staff.
  - Optional customer hub later: `order:{OrderId}` / `customer:{CustomerId}`.

- Server auth and group join
  - Hub requires JWT authentication (`[Authorize]`).
  - Join/Leave model via explicit hub methods, e.g., `SubscribeToRestaurant(restaurantId)` and `UnsubscribeFromRestaurant(restaurantId)`.
  - On subscribe: validate the caller is staff/owner for that restaurant (via repository or `IAuthorizationService` + project policy).

- Notifier implementation
  - `SignalROrderRealtimeNotifier` using `IHubContext<RestaurantOrdersHub>`.
  - Maps abstraction calls to hub group sends:
    - `NotifyOrderPlaced` > `Clients.Group(group).SendAsync("ReceiveOrderPlaced", dto)`.
    - `NotifyOrderPaymentSucceeded/Failed` > `ReceiveOrderPaymentSucceeded/Failed`.
    - `NotifyOrderStatusChanged` > `ReceiveOrderStatusChanged`.
  - Keep send method names stable and minimal; DTO is already versionable.

- Layering
  - Keep the notifier implementation in Web (preferred) or a small Web.Realtime folder. Register it in Web DI after Infrastructure so it overrides the NoOp.
  - Application handlers continue to depend only on the interface.

- Scale-out readiness
  - For single-node dev it works out-of-the-box.
  - Add a switchable backplane (Redis/Azure SignalR Service) behind configuration for prod scale-out (deferred; add placeholders in plan/config section).

---

## Components to Add

1) Web: SignalR service registration and endpoint mapping
- File: `src/Web/DependencyInjection.cs` (method `AddWebServices`)
  - Add: `builder.Services.AddSignalR();`
  - Keep within Web to align with presentation concerns.
- File: `src/Web/Program.cs`
  - Add endpoint mapping: `app.MapHub<RestaurantOrdersHub>("/hubs/restaurant-orders");`
  - Place after pipeline setup (e.g., after exception handling and static files), alongside other Map* calls.

2) Web: Hub class (Restaurant dashboard)
- File: `src/Web/Realtime/Hubs/RestaurantOrdersHub.cs`
  - Namespace: `YummyZoom.Web.Realtime.Hubs`
  - Class: `RestaurantOrdersHub : Hub`
  - Attributes: `[Authorize]`
  - Dependencies (via DI):
    - `IUser` (from Web.Services) to get current user identity
    - `IRoleAssignmentRepository` (Application interface) OR `IAuthorizationService` for policy-based checks
    - `ILogger<RestaurantOrdersHub>`
  - Methods:
    - `Task SubscribeToRestaurant(Guid restaurantId)`
      - Validate caller is owner/staff for `restaurantId` (repo or `IAuthorizationService` with project policies).
      - `await Groups.AddToGroupAsync(Context.ConnectionId, Group(restaurantId));`
    - `Task UnsubscribeFromRestaurant(Guid restaurantId)`
      - `await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(restaurantId));`
  - Helpers:
    - `static string Group(Guid id) => $"restaurant:{id}";`

3) Web: SignalR-backed notifier implementation
- File: `src/Web/Realtime/SignalROrderRealtimeNotifier.cs`
  - Namespace: `YummyZoom.Web.Realtime`
  - Implements: `IOrderRealtimeNotifier` (Application layer interface)
  - Dependencies: `IHubContext<RestaurantOrdersHub>` and `ILogger<SignalROrderRealtimeNotifier>`
  - Behavior: send to group `restaurant:{dto.RestaurantId}` using these server-to-client method names:
    - `ReceiveOrderPlaced`
    - `ReceiveOrderPaymentSucceeded`
    - `ReceiveOrderPaymentFailed`
    - `ReceiveOrderStatusChanged`
  - Note: Keep explicit method names per event for clarity; consider consolidating to `ReceiveOrderEvent` later if needed.

4) Web: DI registration override (replace NoOp with SignalR)
- File: `src/Web/DependencyInjection.cs` (method `AddWebServices`)
  - Add: `builder.Services.AddSingleton<IOrderRealtimeNotifier, SignalROrderRealtimeNotifier>();`
  - Rationale: Web registers after Infrastructure so this implementation overrides `NoOpOrderRealtimeNotifier` (default DI resolves the last registration for a given service when using TryAdd-like patterns; if multiple are present, resolve IEnumerable or ensure ordering—here we intentionally register last).

5) Client contract documentation
- File: `Docs/API-Design/SignalR_Contracts.md` (new)
  - Document hub URL: `/hubs/restaurant-orders`
  - Auth: JWT required
  - Subscription: call `SubscribeToRestaurant(restaurantId)` after connecting
  - Server-to-client events and payload schema: `OrderStatusBroadcastDto`
  - Basic example payloads and notes on versionability

---

## Detailed Flow (end-to-end)

1. Domain event raised on aggregate change (e.g., `OrderAccepted`).
2. Outbox interceptor serializes event, outbox worker publishes via MediatR.
3. `OrderAcceptedEventHandler` (Idempotent) loads order and builds `OrderStatusBroadcastDto`.
4. Handler calls `IOrderRealtimeNotifier.NotifyOrderStatusChanged(dto)`.
5. SignalR notifier sends to `restaurant:{RestaurantId}` group on `ReceiveOrderStatusChanged`.
6. Any connected, authorized staff subscribed to the group receive the update in real-time.
7. If SignalR send fails transiently, the handler throws; Outbox retries later; Inbox ensures no duplication of side-effects.

---

## Security & Authorization (concise guide)

- Model: Keep hubs in Web with `[Authorize]`, use cached claims (no DB lookups), and perform resource-based checks inside hub methods.
- Subscribe flow (resource-based check in method):
  1) `[Authorize]` on `RestaurantOrdersHub`.
  2) In `SubscribeToRestaurant(Guid restaurantId)`, call `IAuthorizationService.AuthorizeAsync(Context.User, resource, policy)` using the project’s IContextualCommand contract:

```csharp
// Minimal resource wrapper for auth (no DB):
private sealed record RestaurantResource(Guid Id) : IContextualCommand
{
  public string ResourceType => "Restaurant";
  public string ResourceId => Id.ToString();
}

public async Task SubscribeToRestaurant(Guid restaurantId)
{
  var resource = new RestaurantResource(restaurantId);

  var owner = await _auth.AuthorizeAsync(Context.User, resource, Policies.MustBeRestaurantOwner);
  var staff = owner.Succeeded
    ? owner
    : await _auth.AuthorizeAsync(Context.User, resource, Policies.MustBeRestaurantStaff);

  if (!staff.Succeeded)
    throw new HubException("Forbidden"); // log and deny

  await Groups.AddToGroupAsync(Context.ConnectionId, Group(restaurantId));
}
```

- Notes
  - Do not query the database in hubs; rely on cached claims (see Auth-Pattern.md).
  - Do not auto-subscribe in `OnConnected`; require explicit subscribe per restaurant.
  - Pass `restaurantId` as a method argument (avoid query string secrets).
  - Broadcast only to `restaurant:{RestaurantId}` groups to prevent leakage.
  - Optional: Add a combined policy (e.g., `MustBeRestaurantMember`) if you prefer a single call; still use resource-based `AuthorizeAsync`.

---

## Observability & Resiliency

- Logging
  - Hub: subscribe/unsubscribe successes/failures; invalid access attempts.
  - Notifier: log failures with EventId/OrderId; rely on Outbox retry.

- Metrics (optional, add placeholders)
  - `realtime_broadcast_attempts_total`, `realtime_broadcast_failures_total`.

- Backpressure & bursts
  - Outbox batching smooths spikes; SignalR send is async. Keep DTO minimal.

- Scale-out
  - Add an optional Redis backplane or Azure SignalR Service later. Provide config scaffolding now (deferred):
    - `UseRedisBackplane` flag and connection string placeholder in Web appsettings.

---

## Incremental Delivery Plan

- PR 1: Add SignalR plumbing
  - Add services.AddSignalR(); create `RestaurantOrdersHub`; map `/hubs/restaurant-orders`.
  - Add subscribe/unsubscribe with repository-based authorization.
  - Docs update for hub usage.

- PR 2: Wire notifier
  - Add `SignalROrderRealtimeNotifier` in Web; register in `AddWebServices()`.
  - Verify functional tests still pass; add a smoke test to assert notifier is resolved as SignalR impl in Web host context.

- PR 3: Hardening & docs
  - Add logs, minimal metrics placeholders, and README snippet for client integration.
  - Optional: basic integration test spinning an in-memory server and connecting a SignalR client to assert group delivery (can be deferred to Phase 1.1 if heavy).

---

## Contracts & Method Signatures (concise)

- Hub
  - `Task SubscribeToRestaurant(Guid restaurantId)`
  - `Task UnsubscribeFromRestaurant(Guid restaurantId)`
  - Server-to-client methods (no client implementation required on server):
    - `ReceiveOrderPlaced(OrderStatusBroadcastDto dto)`
    - `ReceiveOrderPaymentSucceeded(OrderStatusBroadcastDto dto)`
    - `ReceiveOrderPaymentFailed(OrderStatusBroadcastDto dto)`
    - `ReceiveOrderStatusChanged(OrderStatusBroadcastDto dto)`

- Notifier
  - Implement all four methods from `IOrderRealtimeNotifier` to target the group above.

---

## Testing Strategy (Phase 1 scope)

- Unit tests
  - `RestaurantOrdersHub` authorization logic (subscribe succeeds for staff, fails otherwise).
  - `SignalROrderRealtimeNotifier` group name calculation and SendAsync mapping (mock IHubContext).

- Functional tests (lightweight)
  - Existing outbox/inbox tests already validate handler invocation paths.
  - Add a DI override test ensuring the notifier is the SignalR implementation (Web host) rather than NoOp.
  - Optional: in-memory SignalR client test to assert a broadcast reaches a subscriber in the correct group.

---

## Risks & Mitigations

- Duplicate messages: already mitigated via Inbox and handler idempotency.
- Authorization leakage: enforce server-side validation on subscribe; never auto-join based solely on client query string.
- Scale-out delivery gaps: note and plan for Redis/Azure SignalR backplane in a follow-up.
- Versioning drift: `OrderStatusBroadcastDto` is versionable; document any future additions as optional fields.

---

## Acceptance Criteria

- Web exposes `/hubs/restaurant-orders` requiring JWT.
- Staff can subscribe to a specific restaurant and receive broadcasts for that restaurant only.
- Application event handlers continue to work unchanged; Outbox retries failed sends; Inbox prevents duplicates.
- No changes to domain/application contracts; end-to-end flow confirmed by dev smoke tests (and existing functional tests remain green).

---

## Notes & Follow-ups

- Customer hub and backplane integration are explicitly deferred; ensure the current design wont require breaking changes when adding them.
- Keep the NoOp notifier registration in Infrastructure to support non-Web/process contexts; Web overrides it when available.
- Ensure CORS/WebSockets settings are compatible with the dashboard frontend in environments (document any required config).
