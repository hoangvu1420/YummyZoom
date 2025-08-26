# Phase 1 Implementation Plan - Critical Path (Order Placement & Fulfillment)

Purpose: Inventory what already exists vs required scope, then define concrete tasks (no code yet) to complete Phase 1:
1. Orders commands/queries & payment gateway abstraction
2. Webhook handler + idempotency store
3. Restaurant dashboard SignalR updates for OrderPaymentSucceeded → Accepted/Preparing/... lifecycle
4. Coupon usage increment + RestaurantAccount revenue handler

---

## 1. Current State Assessment

### Domain Layer
- Order aggregate: supports lifecycle methods (Accept, Reject, Cancel, MarkAsPreparing, MarkAsReadyForDelivery, MarkAsDelivered, RecordPaymentSuccess/Failure). Emits domain events: OrderCreated, OrderAccepted, OrderRejected, OrderCancelled, OrderPreparing, OrderReadyForDelivery, OrderDelivered, OrderPaymentSucceeded, OrderPaymentFailed.
- RestaurantAccount aggregate: can RecordRevenue / fees / refunds. Emits RevenueRecorded, etc.
- Coupon aggregate: CouponUsed domain event emitted in InitiateOrder handler (not via domain method usage limits are enforced at repository level + manual event publication).
- OrderFinancialService present for pricing & discount logic.

Gaps:
- No domain event handlers wiring revenue on payment success.
- No domain event handler to increment restaurant account upon OrderPaymentSucceeded (or later status) and to publish real-time notifications.
- No read model / projection for coupon usage apart from atomic counters update; need eventual read model if required by UI (optional for Phase 1 unless immediate UI consumption needed).

### Application Layer
- Commands implemented: InitiateOrderCommand (+ validator + handler), HandleStripeWebhookCommand (+ validator + handler).
- Missing order lifecycle commands: AcceptOrder, MarkOrderPreparing, MarkOrderReadyForDelivery, MarkOrderDelivered, CancelOrder, RejectOrder (with validators + auth policies + tests).
- Missing queries: GetOrderById, ListCustomerOrders (basic), ListRestaurantNewOrders / ActiveOrders for dashboard, optionally GetOrderStatus lightweight endpoint / projection.
- No notification handlers for domain events → to trigger SignalR broadcasts & restaurant revenue updates.
- No pipeline / behaviors specific to idempotency for webhook beyond ProcessedWebhookEvent table usage (OK for now, but consider generic IdempotentCommandBehavior for further webhooks/providers).

### Infrastructure Layer
- Payment gateway abstraction implemented (IPaymentGatewayService) with StripeService (create intent, construct webhook event, refund).
- Webhook idempotency store present: ProcessedWebhookEvents table + handler logic.
- Outbox implementation present (OutboxMessages + OutboxProcessor + HostedService) but no event handlers converting domain events to integration messages or updating real-time channels.
- No SignalR infrastructure (no hubs, no registration, no client broadcast service abstraction).
- No repository or projection for restaurant dashboard (e.g., PendingOrdersView / ActiveOrdersView).
- No handler to update RestaurantAccount on confirmed payment (requires locating RestaurantAccount, recording revenue, saving aggregate).

### Web Layer
- Endpoints: /api/orders/initiate and /api/stripe-webhooks already mapped.
- Missing endpoints for lifecycle commands & order queries.
- No SignalR hubs (restaurant dashboard, order status streaming).

### Testing
- Need unit tests for new lifecycle commands & domain event handlers.
- Need functional tests for InitiateOrder (already?) & payment webhook success/failure flow (create -> webhook -> status update).
- Need integration test for Stripe webhook idempotency (second delivery is no-op) and RestaurantAccount revenue application.

---

## 2. Target End State (Definition of Done for Phase 1)
For an order placed with online payment:
1. Customer initiates order → payment intent created → order in AwaitingPayment.
2. Stripe webhook (payment_intent.succeeded) → idempotent processing → order transitions to Placed + OrderPaymentSucceeded event published.
3. Restaurant dashboard receives real-time notification (SignalR) of new placed order.
4. Restaurant accepts order → status → Preparing → ReadyForDelivery → Delivered (via commands) with events each step broadcast to dashboard + optionally customer channel.
5. On payment success (or on Accept step—decision needed) restaurant revenue recorded in RestaurantAccount aggregate.
6. Coupon usage already incremented atomically; ensure no double-application on retries.
7. All actions logged, authorized, and covered by unit + functional tests.

---

## 3. Detailed Task Breakdown

### 3.1 Orders Commands (Lifecycle)
Create commands + validators + handlers (Authorization: restaurant staff / owner; for Cancel may allow customer before Accepted/Preparing):
- AcceptOrderCommand (OrderId, EstimatedDeliveryTime)
- RejectOrderCommand (OrderId, Reason?)
- CancelOrderCommand (OrderId, Actor) – enforce allowed states & actor rules.
- MarkOrderPreparingCommand (OrderId)
- MarkOrderReadyForDeliveryCommand (OrderId)
- MarkOrderDeliveredCommand (OrderId, DeliveredAt? optional override)

Each handler:
- Load order (IOrderRepository)
- Authorization check (current user role vs restaurant).
- Invoke domain method + persist via UnitOfWork.
- Return Result with minimal DTO (OrderId, NewStatus, Timestamps).
- Emit domain events automatically (already in aggregate) → outbox.

### 3.2 Orders Queries
Minimal initial queries (use EF or lightweight Dapper later):
- GetOrderByIdQuery (project to OrderDetailsDto) – includes items, status, totals.
- GetCustomerRecentOrdersQuery (CustomerId, paging)
- GetRestaurantNewOrdersQuery (RestaurantId, status = Placed, paging)
- GetRestaurantActiveOrdersQuery (RestaurantId, status in [Placed, Accepted, Preparing, ReadyForDelivery])
- (Optional) GetOrderStatusQuery (OrderId) – lean endpoint for polling fallback.

### 3.3 Payment Webhook Enhancements
- Ensure webhook handler raises integration log + persists domain events (already records payment success/failure).
- Add functional test: duplicate webhook event returns success and does not duplicate state change.
- Add metric counters (success/failure, duplicates) (observability later if infra in place).

### 3.4 Domain Event Handlers (Application Layer)
Implement INotificationHandlers for emitted events:
- OrderPaymentSucceededHandler: broadcast real-time event + enqueue internal command to auto-add to restaurant dashboard queue (if needed) + trigger RestaurantAccount revenue (if business rule: revenue recognized at payment capture).
- OrderAccepted/Preparing/ReadyForDelivery/Delivered Handlers: broadcast status updates.
- OrderPaymentFailedHandler: broadcast failure / maybe auto-cancel already handled; possibly notify customer.
- CouponUsedHandler (optional): update CouponUsage read model (if required now).

### 3.5 RestaurantAccount Revenue Integration
Decision: Recognize revenue at payment success vs at delivery. (Assumption for Phase 1: at payment success.)
Tasks:
- Add IRepository for RestaurantAccount if not existing (verify presence; implement if missing).
- Event handler for OrderPaymentSucceeded: load RestaurantAccount (by RestaurantId), call RecordRevenue(order.TotalAmount minus platform fee calc TODO), persist.
- (Optional) Introduce PlatformFeePolicy/Service to compute fee & record separate PlatformFeeRecorded event.

### 3.6 SignalR Real-time Layer
- Add Infrastructure/Web: Register SignalR (services.AddSignalR). Add hubs:
  - RestaurantOrdersHub (group per RestaurantId) – methods: ReceiveOrderPlaced, ReceiveOrderStatusChanged.
  - (Optional) CustomerOrderStatusHub (group per OrderId or CustomerId) for customer tracking.
- Add IOrderRealtimeNotifier abstraction + implementation using IHubContext.
- Inject notifier into event handlers to broadcast messages.
- Add auth policy ensuring only staff join restaurant groups; customer joins their order group.
- Map hub endpoints (/hubs/restaurant-orders, /hubs/order-status).

### 3.7 Coupon Usage Read Model (Scope Clarification)
- Since per-user & total usage counters applied atomically already, a separate read model may be deferred unless UI requires aggregate stats. For Phase 1 mark as Deferred unless needed.

### 3.8 Idempotency & Outbox
- Confirm Outbox persists domain events from order aggregate (verify SaveChanges interceptor if any — TODO inspection needed). If missing, add domain event to outbox translation.
- Ensure event handlers are idempotent (e.g., revenue handler checks if revenue for (OrderId) already recorded — could rely on AccountTransaction unique constraint; if absent, create guard).

### 3.9 Endpoints (Web Layer)
Add minimal endpoints under /api/orders:
- POST /{id}/accept, reject, cancel, preparing, ready, delivered.
- GET /{id}
- GET /my-orders (customer context)
- GET /restaurants/{restaurantId}/orders/new
- GET /restaurants/{restaurantId}/orders/active

Return DTOs with: OrderId, Status, Amounts, Key Timestamps.

### 3.10 Testing Plan
Unit Tests:
- Order lifecycle domain methods (edge cases invalid transitions) – many may already exist; add missing.
- Revenue handler: duplicate event ignored.
- Payment webhook handler: success & failure branches.
Functional Tests:
- Customer place order (cash & card path) – card path stops at AwaitingPayment until simulated webhook success.
- Webhook success transitions & idempotency (double send).
- Lifecycle commands (accept → preparing → ready → delivered) + auth checks.
Integration Tests:
- RestaurantAccount revenue applied on payment success (query DB state).
- SignalR broadcast smoke test (optional: host server in-memory, capture messages) – if complex, defer to Phase 1.1.

### 3.11 Observability (Minimal for Phase 1)
- Structured logs already; add log scopes in lifecycle handlers.
- Add minimal metrics counters (orders_placed_total, orders_payment_succeeded_total, webhook_events_processed_total). (If metrics infra not yet setup, place TODO markers.)

### 3.12 Security & Authorization
- Define policies: RestaurantStaff (user has RoleAssignment for RestaurantId), OrderOwner.
- Apply policies to lifecycle endpoints/commands.
- Webhook endpoint stays anonymous but with signature verification.
- Hub authorization: require auth; OnConnected assign to groups based on claims + query params after validating access.

---

## 4. Sequencing & Milestones
Order of execution (incremental PRs):
1. Add missing lifecycle commands + endpoints + basic tests (no real-time yet).
2. Add queries for order retrieval (customer + restaurant).
3. Implement OrderPaymentSucceeded → RestaurantAccount revenue handler (and repository if needed) + tests.
4. Introduce SignalR hubs + notifier abstraction + integrate into existing event handlers.
5. Add remaining status change event handlers broadcasting updates.
6. Add metrics & harden idempotency guards (duplicate revenue, duplicate status broadcast safe).
7. (Optional) Platform fee calculation & recording.

---

## 5. Risk & Mitigation
- Duplicate revenue booking: mitigate with unique AccountTransaction constraint or checking existing transaction for OrderId.
- SignalR group authorization leakage: implement server-side validation before group join.
- Webhook race (status update before command transitions exist): Lifecycle commands operate after payment success so ordering acceptable; ensure Accept only allowed when Status == Placed.
- Outbox missing events: verify infrastructure (TODO) early; add if absent to avoid lost broadcasts.

---

## 6. Open Questions / Assumptions
- Revenue recognition timing (assumed at payment success) – confirm.
- Platform fee % or fixed schedule not yet defined – placeholder service.
- Customer cancellation allowed up to Accepted or also Preparing? Current domain restricts Cancel on Placed/Accepted/Preparing/Ready; we will enforce policy layer for who can cancel (customer before Preparing; staff anytime in allowed states).
- Any SLA for webhook latency? Not specified; outbox/pub sub should be fast enough.

---

## 7. Deliverables Summary
- 6 lifecycle commands + handlers + validators + endpoints + tests.
- 4–5 queries + endpoints.
- Event handlers: payment succeeded/failed, status transitions, revenue, (optional coupon usage projection deferred).
- SignalR hubs + notifier abstraction.
- Revenue booking integration + safety checks.
- Webhook idempotency test coverage.
- Metrics placeholders.

---

## 8. Deferred (Explicit)
- CouponUsage read model projection (unless UI requests statistics now).
- Customer real-time order status hub (fallback is polling) – can be Phase 1.1.
- Platform fee calculation service & fee events (Phase 1.1).
- Search/indexing & dashboard aggregated views (Phase 2 alignment).

---

## 9. Next Immediate Step
Proceed with item (1): implement lifecycle commands Accept/Reject/Cancel/Preparing/Ready/Delivered with validators, handlers, endpoints, and baseline unit + functional tests, verifying domain transitions.

(End of Plan)
