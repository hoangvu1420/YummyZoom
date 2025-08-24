
## ✅ Requirements Checklist
- Identify what already exists (events, infra, patterns) ✔
- List what must be added/changed for each required handler ✔
- Include broadcasting + revenue + idempotency considerations ✔
- Address optional CouponUsed handler ✔
- Note design/architecture considerations (DTOs, repos, testing, performance) ✔
- Call out risks, assumptions, open questions ✔
- No code yet ✔

## What Already Exists

### Domain Events (Order & Related)
- Order lifecycle / payment events defined & emitted in `Order`:
  - `OrderAccepted`, `OrderRejected`, `OrderCancelled`, `OrderPreparing`, `OrderReadyForDelivery`, `OrderDelivered(DeliveredAt)`
  - `OrderPaymentSucceeded`, `OrderPaymentFailed`
- `CouponUsed` domain event exists; sometimes also manually published in `InitiateOrderCommandHandler`.
- Events inherit `DomainEventBase` ⇒ include `EventId` & timestamp (good for inbox idempotency).

### Outbox & Delivery Pipeline
- `ConvertDomainEventsToOutboxInterceptor` captures all domain events and writes `OutboxMessage` rows.
- `OutboxPublisherHostedService` + `IOutboxProcessor` publishes events via MediatR.
- JSON serialization infrastructure (`OutboxJson.Options`) already present.

### Idempotency Infrastructure
- `IdempotentNotificationHandler<TEvent>` base (uses `IInboxStore` + `IUnitOfWork`) with exactly-once semantics.
- Inbox store + EF persistence registered.

### Repositories / Data Access
- `IOrderRepository` available.
- No repository abstraction yet for `RestaurantAccount` (aggregate exists).
- No projection/read model or notifier abstraction for real-time updates.

### Web / Real-time
- No SignalR hubs or abstractions (`IOrderRealtimeNotifier` absent).
- Order endpoints exist for lifecycle transitions; no handlers pushing updates outward yet.

### Financial / Revenue
- `RestaurantAccount.RecordRevenue(Money, OrderId)` exists and raises `RevenueRecorded`.
- No handler yet tying `OrderPaymentSucceeded` to `RecordRevenue`.

### Testing Patterns
- Existing sample event handlers (TodoItem*) show usage of `IdempotentNotificationHandler`.
- Functional/outbox drain helpers referenced in docs (implementation not reviewed here but assumed present per guidelines).

## What Needs to Be Added

### 1. Handler Set (Application Layer / EventHandlers)
Create in `src/Application/Orders/EventHandlers/` (or `.../OrderEvents/`), all deriving from `IdempotentNotificationHandler<...>` (uniform pattern). Each lists business responsibilities & side-effects (all side-effects must be idempotent):
- `OrderPlacedEventHandler`
  - Responsibilities: Broadcast newly actionable order to restaurant dashboards; insert into "New / Awaiting Acceptance" column; start acceptance SLA timer.
  - Side Effects: Broadcast placed status; (future) start acceptance SLA timer; (future) trigger capacity evaluation; (future) enqueue customer confirmation notification.
  - Exclusions: No revenue, no payment transaction mutation (already finalized), no ETA computation (done at Accept).
- `OrderPaymentSucceededEventHandler`
  - Responsibilities: Record that payment was successful (for analytics & potential pending revenue) and inform internal payment status observers.
  - Side Effects: Broadcast payment confirmation (internal channel or enrich next OrderPlaced broadcast if closely sequenced); (future) create pending revenue entry (NOT recognized); (future) trigger fraud/risk evaluation async.
  - Exclusions: Does NOT itself surface order on restaurant acceptance board (that's OrderPlaced); no `RecordRevenue` call; no customer notification yet.
- `OrderPaymentFailedEventHandler`
  - Responsibilities: Inform restaurant (optional) & mark order as failed payment path so they ignore it; allow customer re-payment flow.
  - Side Effects: Broadcast failure; (future) enqueue customer notification; (future) increment payment failure metric; (future) start grace period timer before auto-cancel.
  - Exclusions: No revenue, no refunds (none captured).
- `OrderAcceptedEventHandler`
  - Responsibilities: Transition visible state to Accepted; start kitchen prep SLA tracking; calculate and send ETA to the customer.
  - Side Effects: Broadcast status; (future) start prep SLA timer; (future) update order throughput metrics; (future) clear any acceptance reminder timer.
  - Exclusions: Still no revenue recognition.
- `OrderPreparingEventHandler`
  - Responsibilities: Communicate that cooking has begun; adjust ETA logic.
  - Side Effects: Broadcast status; (future) recalc EstimatedDeliveryTime; (future) start preparation-to-ready SLA timer.
- `OrderReadyForDeliveryEventHandler`
  - Responsibilities: Indicate order is ready for pickup/delivery dispatch; adjust ETA for delivery.
  - Side Effects: Broadcast status; (future) notify delivery assignment subsystem; (future) start ready-to-delivered SLA timer.
- `OrderDeliveredEventHandler`
  - Responsibilities: Finalize order lifecycle; recognize revenue.
  - Side Effects: Call `RestaurantAccount.RecordRevenue` (recognition); broadcast delivered status; (future) convert pending revenue → recognized; (future) trigger loyalty points awarding; (future) emit customer feedback request event; (future) close any outstanding SLA timers.
  - Exclusions: No further status transitions allowed after broadcast.
- `OrderRejectedEventHandler`
  - Responsibilities: Inform customer & restaurant dashboards that order was rejected early.
  - Side Effects: Broadcast status; (future) initiate automatic full refund if payment captured (edge case); (future) release pending revenue (if any); (future) restore coupon usage if business rules allow.
  - Exclusions: No revenue recognition; no duplicate refund attempts.
- `OrderCancelledEventHandler`
  - Responsibilities: Reflect user/staff cancellation; differentiate from rejection (post-accept or in-progress).
  - Side Effects: Broadcast status; (future) partial refund logic if already captured & policy allows; (future) release pending revenue; (future) coupon restoration rules check.
  - Exclusions: No revenue recognition; avoid double refunds.
- (Optional/Deferred) `CouponUsedEventHandler`
  - Responsibilities: Maintain coupon analytics / usage projection.
  - Side Effects: Update `CouponUsage` projection table (insert/update); (future) broadcast remaining availability to admin dashboards; (future) trigger depletion alert if threshold reached.
  - Exclusions: No direct financial mutations.

### 2. Supporting Abstractions
- `IRestaurantAccountRepository`
  - Methods: `GetByRestaurantIdAsync(RestaurantId)`, `AddAsync(RestaurantAccount)`, `UpdateAsync(...)`.
- `IOrderRealtimeNotifier` abstraction
  - Methods (initial): 
    - `Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, CancellationToken)`
    - `Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, CancellationToken)`
    - `Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, CancellationToken)`
  - Backed later by SignalR (implemented in Step 3.6). For now can be a no-op stub to unblock handlers.
- Broadcast DTO(s):
  - `OrderStatusBroadcastDto` (OrderId, Status, LastUpdateTimestamp, EstimatedDeliveryTime, ActualDeliveryTime, DeliveredAt?, Maybe RestaurantId, OrderNumber).
  - Possibly separate `OrderPaymentResultDto` if semantics diverge.

### 3. Revenue Flow Enhancements
- Decision: revenue is **recognized at `OrderDelivered`** (performance obligation satisfied). 
- Optional (future) pending tracking: at `OrderPaymentSucceeded` record a non-recognized pending amount (NOT added to `CurrentBalance`) for dashboard visibility.
- In `OrderDeliveredEventHandler`:
  - Load `Order`.
  - Derive `Money` amount (gross for now; platform fee TODO).
  - Load or create `RestaurantAccount`.
  - Call `RecordRevenue` (recognition).
  - Persist via `IUnitOfWork` inside idempotent handler transaction.
- If cancellation/refund before delivery: no recognized revenue; pending (if implemented) simply released (future scope).

### 4. Optional / Future
- If repeated status broadcasts expected, consider consolidating into one generic `OrderStatusChangedEventHandler` handling multiple event types via pattern matching (optional optimization).
- Add integration message publication (e.g., to a message bus) later—place extension point comments.

### 5. Testing Artifacts
- Unit tests for each handler:
  - Ensures idempotency (second invocation doesn’t duplicate revenue / inbox entry).
  - Ensures proper repository interaction & notifier calls.
- Functional test scenario:
  - Simulate payment success → drain outbox → assert revenue + broadcast (mock).
  - Simulate duplicate outbox processing attempt (drain twice) → single revenue entry.
- (If adding stub notifier) Provide test double verifying method invocation counts.

## Implementation Considerations

### Idempotency
- All handlers use `IdempotentNotificationHandler` (events already have `EventId`).
- Ensure `RevenueRecorded` itself does not cause duplicate revenue if handler retried mid-transaction—it’s inside idempotent unit-of-work with inbox check.

### Data Loading / N+1
- Events only carry `OrderId`; handlers must load order (single lookup). Acceptable; optimization (enrich event) can be deferred.
- Broadcasting likely needs RestaurantId (present on Order aggregate) and maybe simplified monetary totals.

### Performance
- Keep handlers thin (load order + maybe restaurant account + one mutation). Avoid large graph retrieval—ensure repository doesn’t eager-load unnecessary navigation collections.
- Use cancellation tokens throughout.

### Failure & Retry
- If revenue booking fails (e.g., repository transient error), outbox retry will re-run handler; idempotent guard prevents double application once inbox insert committed.
- Logging: include `EventId` + `OrderId` for correlation.

### Transaction Boundary
- IdempotentNotificationHandler wraps logic + inbox write + side-effects in a single transaction using `IUnitOfWork` (EF context). Ensure `RestaurantAccount` and `Order` share same DbContext to avoid distributed transactions.

### Repository Design
- `IRestaurantAccountRepository.GetByRestaurantIdAsync(RestaurantId)` for fast fetch—indexed column needed (check DB migration later).
- If auto-create desired, handler can create + add when null (validate business rules).

### Real-time Broadcasting
- For now, create abstraction; actual SignalR hub & registration added in Step 3.6.
- Handler shouldn’t depend on SignalR types directly (keeps layering clean).
- No-op implementation can be registered short-term to avoid null references.

### DTO Shape Stability
- Broadcasting contract should be versionable; keep minimal fields initially.
- Consider including `OccurredOnUtc` from event for chronological ordering.

### CouponUsed Handler (Optional)
- If implemented now: maintain table `CouponUsageProjections` (not present). Otherwise, defer and document.
- Current manual `mediator.Publish` call bypasses outbox. For consistency, future refactor: rely solely on aggregate-raised event & remove manual publish; ensure coupon usage increments still atomic.

### Naming & Organization
- Folder: `src/Application/Orders/EventHandlers`
  - Files: `OrderPaymentSucceededEventHandler.cs`, etc.
- If generic: `OrderStatusEventHandlers.cs` with multiple small classes inside (optional).
- Keep each handler < ~80 lines.

### Logging
- Standard log template: `Handling {EventName} (EventId={EventId}, OrderId={OrderId})`
- On success: `Handled {EventName} (EventId=..., OrderId=...)`
- On duplicate (inbox hit): rely on base behavior or optionally log debug.

### Observability Hooks (Future)
- Insert TODO for metrics increments (e.g., counters) where infra not yet ready.

### Serialization / Value Objects
- Events currently simple (OrderId, DeliveredAt). No extra converters needed now.

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Missing restaurant account leads to revenue loss | Auto-create or log + retry; decide policy (recommend auto-create). |
| Duplicate revenue due to manual handler or event re-emission | Idempotent handler + (optional) DB unique constraint on (RestaurantAccountId, RelatedOrderId, TransactionType). |
| Broadcast failures (e.g., SignalR offline) block revenue | Separate concerns; treat notifier failure as non-fatal (wrap and log) unless business requires atomic coupling (likely not). |
| Manual `mediator.Publish(CouponUsed)` creates race with outbox pattern | Document now; refactor later when projection added. |
| Future integration bus introduction duplicates semantics | Abstract notifier/interface now to minimize churn. |

## Open Questions / Assumptions
- Assumption (updated): Revenue recognized at `OrderDelivered`; payment success may later create a pending (non-recognized) entry.
- Should broadcasts include customer-facing channel now? (Assume restaurant-only for Phase 1.)
- Create restaurant account lazily vs pre-provision? (Assume lazy create in handler.)
- Are platform fees applied now? (Assume not; record gross; leave TODO.)
- Is failure to broadcast acceptable (best-effort)? (Assume yes; log warning.)

## Proposed Next-Step Sequence (For Implementation PRs)
1. Add `IRestaurantAccountRepository` + implementation + DI registration.
2. Add `IOrderRealtimeNotifier` + no-op implementation + DI registration.
3. Add broadcast DTO(s).
4. Implement all event handlers (status + payment + failure).
5. Add unit tests for handlers & revenue idempotency.
6. Functional test: payment success flow → drain outbox → assert revenue & single processing.
7. (Optional) Implement CouponUsed handler or defer with doc note.

## Deferred / Explicitly Out of Scope for This Step
- Real SignalR hubs & group authorization (Step 3.6).
- Metrics counters instrumentation.
- Coupon usage projection & refactor away manual publish.
- Platform fee calculation service.
- Integration message publishing to external systems.

## Summary
Core infrastructure (events, outbox, idempotent base) is in place; missing pieces are repository + notifier abstractions and the actual handlers linking domain events to revenue booking and real-time messaging. The outlined additions are small, cohesive, and align with existing patterns, minimizing future refactor when SignalR and projections are introduced.
