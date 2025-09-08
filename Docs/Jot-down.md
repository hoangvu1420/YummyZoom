### Phase 3.7 scope and flow fit

- Ready-to-confirm gating
  - Business rule: TeamCart transitions to ReadyToConfirm only when every member has a committed payment and all online payments have succeeded.
  - Source: domain raises `TeamCartReadyForConfirmation(TeamCartId, Total, CashPortion)`. Inbox handler updates RT VM and notifies.

- Conversion to Order
  - Entry point: `ConvertTeamCartToOrderCommand` (host-only).
  - Pre-conditions: TeamCart must be `ReadyToConfirm`; cart must not be expired or already converted.
  - Orchestration: In one transaction, compute delivery fee/tax/discount, reconcile member payments, call `TeamCartConversionService.ConvertToOrder`, persist Order + updated TeamCart, publish events.
  - Post-conditions: TeamCart moves to Converted; `TeamCartConverted` event drives VM deletion and broadcasts; new Order references `SourceTeamCartId`.

- After-conversion read model
  - RT VM is deleted; clients switch to Order channels and details APIs.

### Component-level business logic (Phase 3.7)

- TeamCartReadyForConfirmation event handler (Inbox)
  - Update Redis VM readiness flag (if maintained), broadcast `NotifyReadyToConfirm`.
  - Idempotent; no DB changes.

- ConvertTeamCartToOrderCommand (+ validator, auth)
  - Auth: `[Authorize]`, host-only.
  - Load TeamCart (must be `ReadyToConfirm`), load Restaurant/menu data needed for fees/tax.
  - Compute:
    - Subtotal from cart items (with captured snapshot prices and customizations).
    - Tip from cart.
    - Delivery fee from restaurant/service policy.
    - Tax from jurisdiction/rate applied to taxable base.
    - Discount amount (recompute from `AppliedCouponId` + validation).
  - Reconcile payments:
    - Sum member committed payments; verify equals expected total minus delivery/tax/tip adjustments as designed.
    - Validate all online payments are Paid; COD portions are well-defined.
  - Invoke `TeamCartConversionService` to produce Order aggregate (carry snapshots, totals, coupon, tip, `SourceTeamCartId`).
  - Persist both aggregates atomically: `Order.AddAsync`, `TeamCart.MarkAsConverted()`.
  - Return OrderId + any client response needed.

- TeamCartConverted event handler (Inbox)
  - Delete VM via `ITeamCartStore.DeleteVmAsync`.
  - Real-time broadcast `NotifyConverted`.
  - Idempotent; no DB changes.

### Implementation notes

- CQRS and transactions
  - Keep command handler thin; perform all financial math server-side.
  - Wrap conversion in a single `IUnitOfWork` transaction: persist Order + updated TeamCart; rely on outbox for downstream effects.

- Financial rigor
  - Use immutable snapshots from items for subtotal and customization pricing; do not hit live prices.
  - Recompute discount with coupon entity logic; validate scope and window; fail fast if invalid.
  - Compute tax on prescribed base; be explicit which components are taxable.
  - Reconcile: sum(member payments) == final total; if COD+Online mix, verify online sums match the set of PaidOnline entries and residual is COD.

- Idempotency
  - Add a conversion guard in domain: only convert once; subsequent calls are no-ops/fail with specific error.
  - Inbox handlers for `ReadyForConfirmation` and `Converted` use the shared IdempotentNotificationHandler.

- Data linkage
  - Set `Order.SourceTeamCartId`.
  - Record applied coupon id and any needed usage finalization per existing order pattern.

- Real-time and cache
  - Do not mutate Redis from commands. VM deletion and broadcasts happen in event handlers.
  - Ensure deletion on Converted and on Expired (Phase 3.8).

- Validation and auth
  - Host-only command; enforce membership/ownership policy.
  - Validate address and delivery constraints at conversion time.
  - Guard against conversion when TeamCart is not `ReadyToConfirm`.

### Patterns to follow

- Consistency with Orders
  - Use existing order payment abstractions and discount validation logic.
  - Use outbox/inbox + idempotent handlers.
  - Structured logs with `cartId`, `orderId`, `actingUserId`.

- Redis store discipline
  - All VM changes via handlers; atomic store operations; ETag/version increment.

- Error handling
  - Use `Result` pattern; surface domain errors like `InvalidStatusForConversion`, `PaymentMismatchDuringConversion`, `CannotConvertWithoutPayments`.

### Sufficiency assessment

- The Phase 3.7 design is sufficient:
  - Clear precondition (`ReadyToConfirm`), single authoritative conversion pathway, atomic persistence, and clean RT VM lifecycle.
  - Uses existing `TeamCartConversionService`, coupon reconciliation guidance, and inbox/outbox patterns already in the project.

- Key risks and recommendations
  - Delivery/tax computation: ensure deterministic, snapshot-based inputs; add unit tests for boundary cases.
  - Coupon reconciliation: finalize usage on order success (align with Phase 1). If per-user/total usage counters are used, wrap updates idempotently or leverage outbox-driven “coupon used” event to avoid double increments.
  - Conversion idempotency: add a unique conversion token or explicit domain guard to prevent duplicate orders if the command is retried.
  - Payment reconciliation: include a precise check that the sum of `MemberPayments` equals the final total (including tip, minus discount, plus delivery/tax) and that all `Online` entries are in PaidOnline state.
  - Observability: add metrics for conversion attempts/success/fail, mismatch errors, and VM deletion.
  - API ergonomics: response should return the new `OrderId` and a small handoff payload so clients can switch contexts smoothly.

If you want, I can scaffold:
- `TeamCartReadyForConfirmationEventHandler` (inbox) to set VM readiness and notify.
- `ConvertTeamCartToOrderCommand` + validator/handler using the above rules.
- `TeamCartConvertedEventHandler` to delete VM and notify.