I'll scan the Phase 3 outline and locate the existing order payment processing code to compare patterns and evaluate the "Member Payments" design. Then I'll synthesize an assessment with implementation notes and recommendations.

[5 tools called]

I reviewed the Phase 3 outline with focus on 3.6, and cross-checked it against the implemented order payment flow (initiation, webhook handling, domain events, realtime notifications) and current TeamCart artifacts.

- I’ve verified the order payment patterns (gateway abstraction, initiation + webhook finalization, idempotency, domain events, real-time notifications) and the TeamCart foundations (aggregate, events, Redis store, event handlers) are already available and consistent with the approach in Phase 3.

### Assessment: Design sufficiency for Member Payments (Phase 3.6)

- Overall sufficiency: The Phase 3.6 design is sound and closely mirrors the order payment architecture. It leverages:
  - Existing `IPaymentGatewayService` abstraction for payment intents.
  - Asynchronous finalization via Stripe webhooks and idempotent processing.
  - Domain event-driven updates (inbox/outbox) to the Redis real-time VM and SignalR notifications.
  - Locked state precondition for payments to avoid moving financial targets.
- Planned mechanics align with orders:
  - Initiation-only command for online payments with client secret returned; no aggregate mutation until webhook confirmation.
  - Idempotent webhook orchestrator that looks at metadata to route to TeamCart, then calls aggregate methods to record outcomes and emits events.
  - COD is a local aggregate mutation and consistent with Result-pattern and handler-driven VM updates.
- Key detail: The webhook design relies on PaymentIntent metadata `{ teamcart_id, member_user_id }` to avoid lookups by intent id. This is acceptable and keeps coupling low, provided we enforce consistent metadata creation and robust verification in the webhook layer.
- Redis VM path and notifier usage are consistent with prior phases: handlers update the store and broadcast, commands remain transactional and free of cache side effects.

### Comparison to implemented Order payment modules

- Initiation and webhook finalization:
```69:113:src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs
            if (order is null)
            {
                _logger.LogWarning("Order not found for payment gateway reference ID: {PaymentGatewayReferenceId}. Event might not be order-related.", 
                    webhookEvent.RelevantObjectId);
                
                // Still mark as processed to avoid reprocessing
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }
...
            var processingResult = webhookEvent.EventType switch
            {
                "payment_intent.succeeded" => ProcessPaymentSuccess(order, webhookEvent.RelevantObjectId),
                "payment_intent.payment_failed" => ProcessPaymentFailure(order, webhookEvent.RelevantObjectId),
                _ => ProcessUnsupportedEvent(webhookEvent.EventType)
            };
```
- Domain-side confirmation on success/failure:
```592:640:src/Domain/OrderAggregate/Order.cs
    public Result RecordPaymentSuccess(string paymentGatewayReferenceId, DateTime? timestamp = null)
    {
        if (Status != OrderStatus.AwaitingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }
...
        AddDomainEvent(new OrderPaymentSucceeded(Id));
        AddDomainEvent(new OrderPlaced(Id));
        return Result.Success();
    }
...
    public Result RecordPaymentFailure(string paymentGatewayReferenceId, DateTime? timestamp = null)
    {
        if (Status != OrderStatus.AwaitingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }
...
        AddDomainEvent(new OrderPaymentFailed(Id));
        AddDomainEvent(new OrderCancelled(Id));
        return Result.Success();
    }
```
- Phase 3.6 mirrors these patterns with per-member scoping and Redis VM updates via handlers. Good reuse potential for idempotency/gateway abstractions.

### Implementation notes (Phase 3.6)

- Commands
  - CommitToCodPaymentCommand: enforce Locked state and membership; compute member subtotal; aggregate `CommitToCashOnDelivery(userId, amount)`; persist; emit `MemberCommittedToPayment`.
  - InitiateMemberOnlinePaymentCommand: compute member subtotal; call `IPaymentGatewayService.CreatePaymentIntentAsync(amount, metadata: { teamcart_id, member_user_id })`; return `client_secret`; do not mutate aggregate.
- Webhook
  - New `HandleTeamCartStripeWebhookCommandHandler`: verify signature, deserialize event, idempotency check, route by metadata `teamcart_id` (no `order_id`), find TeamCart by id, call:
    - `RecordSuccessfulOnlinePayment(userId, amount, transactionId)` on `payment_intent.succeeded`.
    - `RecordFailedOnlinePayment(userId, amount, transactionId)` on failures.
  - Persist aggregate, mark webhook as processed, rely on event handlers to update Redis and broadcast. Ensure the amount used for recording matches the aggregate-computed member subtotal to protect against tampering; reject if mismatch.
- Event handlers (Inbox, idempotent)
  - `MemberCommittedToPayment`, `OnlinePaymentSucceeded`, `OnlinePaymentFailed`: update `ITeamCartStore` with member payment status and committed amount; recompute ReadyToConfirm; broadcast.
- Validation and auth
  - Validators: Locked-only for payment actions; member-only for commit/initiate; non-negative amounts; consistent currency.
  - Guard against duplicate commits/initiations per member if already in terminal payment state.
- Real-time VM updates
  - Reflect per-member fields: `PaymentStatus`, `CommittedAmount`, `OnlineTransactionId?`, and aggregate readiness. Keep VM discount/tip aligned with domain events from 3.5.

### Patterns to follow

- CQRS + Result pattern; handlers orchestrate, domain enforces invariants; no side-effects in commands beyond persistence.
- Outbox/inbox idempotency for all domain-event-driven Redis mutations and notifications.
- Payment gateway abstraction: reuse `IPaymentGatewayService` and idempotency keys.
- Webhook idempotency store: reuse the same Processed Events table/mechanism as orders.
- Redis atomic writes (CAS or Lua) and sliding TTL refresh; version/ETag increments for clients.
- SignalR notifier mirrors the order notifiers; group name `teamcart:{id}`.

### Recommendations and gaps to address

- Webhook routing
  - Extend the existing webhook endpoint to branch by metadata: if `order_id` present → existing order handler; if `teamcart_id` present → TeamCart handler. Ensure a shared signature verification path and a single idempotency store with a partition key for source type.
- Idempotency store reuse
  - Extract/rename the processed events repository used in order webhooks to a generic interface in Application (e.g., `IWebhookEventInbox`) and reuse it for TeamCart.
- Payment intent metadata schema
  - Standardize metadata keys: `source=teamcart`, `teamcart_id`, `member_user_id`, `tenant/env`, `purpose=member_payment`. Validate presence strictly in handler.
- Amount consistency checks
  - When recording online success via webhook, recompute the member’s subtotal from the aggregate; if mismatch with intent amount, flag and do not mark as succeeded. Log and treat as failure/invalid; optionally queue for manual review.
- Mixed payments policy
  - Confirm supported mixes (COD and online) at the member level; ensure aggregate prohibits double-commit (e.g., COD after online success).
- Retry behavior and dead-lettering
  - On transient errors in webhook handling, return failure to allow retry; on persistent domain validation failures, mark processed with warning logs to avoid redelivery loops.
- Observability
  - Add structured logs with `cartId`, `memberUserId`, `paymentIntentId`, `eventId`; counters for member online payments initiated/succeeded/failed; alert on repeated amount mismatches.
- Security
  - Verify Stripe signature for TeamCart webhook route; enforce Locked state before any payment commits; do not accept client-provided amounts for commits; derive on the server.
- Testing
  - Functional tests:
    - COD commit happy path and re-commit rejection.
    - Online initiation returns client secret; webhook success drives ReadyToConfirm when all members paid/committed.
    - Idempotent webhook replays; failure path for mismatched amounts; mixed COD+Online combinations.
  - Integration tests:
    - `RedisTeamCartStore` updates for payments; CAS contention; TTL preserved; notifier called once under inbox idempotency.

### Priority checklist to implement Phase 3.6

- Implement commands: `CommitToCodPaymentCommand`, `InitiateMemberOnlinePaymentCommand` (validators, auth).
- Implement `HandleTeamCartStripeWebhookCommandHandler` with shared signature verification and idempotency reuse.
- Add/confirm domain methods on `TeamCart` for recording online success/failure with member scoping and readiness recomputation.
- Add inbox handlers for `MemberCommittedToPayment`, `OnlinePaymentSucceeded`, `OnlinePaymentFailed` to update `ITeamCartStore` and broadcast.
- Extend webhook endpoint routing to delegate to TeamCart handler when metadata indicates TeamCart.
