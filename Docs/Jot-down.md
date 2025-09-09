I'll locate and read the Phase 3 outline and scan the codebase for TeamCart/cart-related components to assess Phase 3.8 against business needs. Then I'll synthesize findings and recommendations.

[5 tools called]

I reviewed the Phase 3 outline (with focus on 3.8) and scanned the TeamCart domain, application event-handlers, repository, and tests to ground the assessment.

### Assessment of Phase 3.8 design (Expiration)
- **Overall sufficiency**: The design is sound and consistent with the project’s Clean Architecture and existing TeamCart patterns. It cleanly separates responsibilities:
  - A background hosted service to select and expire eligible carts in batches.
  - Aggregate-level `MarkAsExpired` mutation emitting `TeamCartExpired`.
  - An idempotent event handler to delete the Redis VM and notify via real-time.
  - Tests to ensure idempotency and cleanup.
- **Alignment with business rules**: It respects lifecycle invariants (terminal state), preserves Redis as the authoritative real-time store while keeping SQL as the source of truth, and leverages the outbox/inbox pattern for durable consistency.
- **Key risks to address**:
  - **Race with conversion/payment** near the expiration boundary.
  - **Clock skew** between DB time and scheduler time.
  - **Redis TTL vs DB truth** drift if VM TTL expires early or keyspace notifications are enabled later.
  - **Idempotency** across re-runs or process restarts.

### Implementation notes for Phase 3.8
- **Hosted service behavior**
  - Prefer a DB-driven scan: query carts with `Status in (Open, Locked, ReadyToConfirm)` and `ExpiresAt <= now - grace`.
  - Use a modest batch (e.g., 200) with a short delay between batches; make cadence configurable.
  - Wrap each cart expiration in a unit-of-work and update through `ITeamCartRepository`, not direct SQL.
  - Do not mutate Redis in the service; rely on the event handler for `TeamCartExpired`.
- **Aggregate and events**
  - Ensure `TeamCart.MarkAsExpired()` is a guard-checked transition from non-terminal states, emits `TeamCartExpired`, and is idempotent.
- **Event handler**
  - Implement an inbox-guarded handler: delete `ITeamCartStore` VM, then `NotifyExpired`.
  - Handler must be idempotent: VM delete should tolerate missing keys; notification should be deduped via inbox/outbox idempotency.
- **Testing focus**
  - Functional tests: a cart past `ExpiresAt` transitions to `Expired`; re-running the service makes no further changes.
  - Integration tests: VM deleted; SignalR notifier invoked exactly once; inbox guarantees idempotency.
  - Concurrency test: if a cart converts while the expiration job runs, only one terminal state wins; the loser should no-op safely.

### Patterns to follow (consistent with the codebase)
- **DDD/CQRS**: Mutate via aggregate methods; commands/services orchestrate; do not mix read model updates in commands.
- **Outbox/inbox**: Durable VM update and notifications should occur in idempotent handlers after DB commit.
- **Atomic Redis ops**: Use the existing `ITeamCartStore` atomic methods; deletion should be simple and idempotent.
- **Feature flags**: Respect `Features:TeamCart` for hosted service registration if required by ops policy.
- **Validation**: Keep expiration strictly server-driven; clients should not be able to force expiry.

### Recommendations and clarifications
- **Add a small grace window** (e.g., 30–120 seconds) when selecting expired carts to reduce races with in-flight conversion/payment. Make it configurable.
- **Guard terminal state transitions**:
  - If `Status` is already `Converted` or `Expired`, skip in the service.
  - In `MarkAsExpired`, early-return if terminal; ensure no changes to items/payments post-expiration.
- **Observability**
  - Metrics: number of expired carts per run, per-restaurant distribution, VM deletions, handler idempotency hits, and “races prevented.”
  - Logs: include `cartId`, `restaurantId`, `expiresAt`, `selectedAt`, and decision (expired/skip reason).
- **Concurrency control**
  - For each candidate, reload “for update” (or rely on EF concurrency tokens) before calling `MarkAsExpired` to avoid double-expiry with multiple nodes.
- **Client semantics**
  - Ensure API endpoints return appropriate errors after expiration (e.g., 410 Gone or domain-specific error) for any subsequent mutations (add item, lock, payment, convert).
  - The real-time hub should broadcast `Expired` so UIs disable inputs immediately.
- **Configuration**
  - Make cadence, batch size, grace period, and retry/backoff configurable via `appsettings`.
  - Consider back-pressure: if a run exceeds cadence window, log and adjust batch in next run.
- **Redis reconciliation**
  - Keep DB scan as source of truth. If keyspace notifications are optionally added later, treat them as hints only; do not rely solely on TTL expirations of the VM.
- **Stripe webhook overlap**
  - If a webhook lands after expiration, handler should no-op aggregate mutations and still log for auditing. Consider notifying the member that payment was ignored due to expiration.
- **Operational safety**
  - Provide an admin script/command to “force expire” a cart and a separate read-only diagnostic endpoint to inspect expiration candidates in production.

If helpful, I can draft the `TeamCartExpirationHostedService`, the event handler for `TeamCartExpired`, and test scaffolding next.

- I finished reading the Phase 3 outline and related TeamCart components. I’ll proceed to implement Phase 3.8 next if you want me to create the hosted service, handler, and tests.