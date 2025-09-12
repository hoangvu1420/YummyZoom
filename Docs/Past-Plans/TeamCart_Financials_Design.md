# TeamCart Financials — Even Split Model (Proposed)

This document specifies an end-to-end, even-split financial model for TeamCart that is rational, predictable, and easy to communicate to customers and client apps. It replaces assumptions in current code where needed and clarifies behavior for coupons, tips, fees, taxes, rounding, and mixed payment methods.

## Objectives

- Evenly split all cart-level adjustments (delivery, service/platform fees, tax, tip, and cart-wide discounts) across participating members.
- Preserve intuitive fairness: each member pays their own items plus an equal share of cart-level adjustments, minus an equal share of cart-wide discounts.
- Provide deterministic rounding that guarantees per-member amounts sum to the cart grand total.
- Support both online and COD payments per member, with final reconciliation at conversion.
- Keep UX transparent with a quoted per-member amount after lock.

## Definitions

- Item subtotal (per member m): sum of line totals for items added by m (including item-level customization adjustments).
- Participating members: members with item subtotal > 0 at lock time. Guests with zero items do not share adjustments.
- Cart-level adjustments: delivery fee, service/platform fees, tip, taxes, and cart-wide discounts. Item-specific discounts remain on their lines before aggregation.
- Currency: a TeamCart operates in a single currency (enforced at creation).

## Computation Timeline

1) Open (pre-lock)
- Members add/remove items. No authoritative quotes; clients may show estimated totals with caveats.

2) Locked (authoritative quote produced)
- Freeze membership for splitting (participating members only: subtotal > 0).
- Compute authoritative quote:
  - Line/item computations (including item-level discounts)
  - Cart-wide coupon(s)
  - Tip (amount or percentage), delivery and service fees
  - Taxes (see Tax Base Policy)
- Produce per-member totals using Even Split Allocation with deterministic rounding.
- Persist per-member quote amounts in the cart state (used for payment intents and validations).
- Any change to tip/coupon after lock triggers re-quote and invalidates unpaid member quotes.

3) Conversion
- Validate sum of all member payments/commitments equals the quoted grand total.
- If mismatch due to partial payments or late adjustments, conversion is blocked until resolved or re-quoted.

## Calculation Details

Inputs
- For each member m: ItemSubtotal[m]
- Cart-wide fees: DeliveryFee, ServiceFee (optional), OtherFees[]
- Tip: TipAmount (absolute) or TipPercent over the chosen base
- Coupons/discounts: Mix of item-level and cart-wide; cart-wide treated as a single DiscountCart amount for allocation
- Tax Base Policy (configurable per jurisdiction; default below)

Step A — Base amounts
- SubtotalCart = sum(ItemSubtotal[m]) over participating members
- DiscountItemTotal = sum of all item-level discounts already applied on lines
- SubtotalAfterItemDiscounts = SubtotalCart - DiscountItemTotal

Step B — Cart-wide coupon
- Compute DiscountCart according to coupon rule (e.g., percent or fixed amount, with caps)
- Ensure 0 <= DiscountCart <= SubtotalAfterItemDiscounts

Step C — Tip
- If TipPercent is provided: TipAmount = round_to_minor_units(TipPercent × SubtotalAfterItemDiscounts)
- Else use explicit TipAmount provided by host

Step D — Fees
- FeesTotal = DeliveryFee + ServiceFee + sum(OtherFees)

Step E — Tax (default policy)
- Default taxable base: TaxableBase = SubtotalAfterItemDiscounts + FeesTotal + TipAmount
- TaxAmount = round_to_minor_units(TaxRate × TaxableBase)
- Note: Different locales may exclude delivery or tip from tax; expose policy flags to adjust the base accordingly.

Step F — Grand total
- GrandTotal = SubtotalAfterItemDiscounts + FeesTotal + TipAmount + TaxAmount

## Even Split Allocation

- Participants = { m | ItemSubtotal[m] > 0 }, n = |Participants|
- Evenly split all cart-level adjustments across Participants:
  - ShareFees[m] = FeesTotal / n
  - ShareTip[m] = TipAmount / n
  - ShareTax[m] = TaxAmount / n
  - ShareCartDiscount[m] = DiscountCart / n (reduces member totals)
- Per-member total before rounding:
  - RawTotal[m] = ItemSubtotal[m] - (item-level discounts already included) + ShareFees[m] + ShareTip[m] + ShareTax[m] - ShareCartDiscount[m]
  - Implementation note: ItemSubtotal[m] should reflect item-level discounts to avoid double counting.

### Rounding Strategy (deterministic, sum-preserving)
- Work in minor units (e.g., cents) for all split values.
- For each split X in {FeesTotal, TipAmount, TaxAmount, DiscountCart}:
  - q = floor(X_cents / n), r = X_cents mod n
  - Assign q to all members for that component
  - Distribute the remaining r cents by a stable order (e.g., by ascending Guid of UserId) for that component
- Compute MemberTotalCents[m] = ItemSubtotalCents[m] + ShareFeesCents[m] + ShareTipCents[m] + ShareTaxCents[m] - ShareDiscountCents[m]
- Guarantee: sum(MemberTotalCents[m]) == GrandTotalCents

### Edge Cases
- Single participant (n=1): member pays the entire cart total.
- Zero item members: excluded from split (they pay nothing) and cannot convert until they add/remove to align with policy.
- Excess discount: If DiscountCart equals SubtotalAfterItemDiscounts, members pay only fees+tip+tax (still split evenly). If discount exceeds base (should not), cap at base.
- Negative totals: Floor each MemberTotal[m] at zero after rounding. If capping creates shortfall vs GrandTotal, reduce the highest member’s discount share(s) to compensate, keeping sum consistency.

## Payment Flows

- Online payment initiation per member uses the quoted MemberTotal[m]. Payment intents must embed `teamcart_id` and `member_user_id` and the quoted amount.
- Webhooks validate amount against the member’s quoted amount (not recomputed from items) and record results.
- COD: `CommitToCodPayment` records the quoted MemberTotal[m] for the acting member.
- Mixed payments: Some members online, some COD; conversion requires all MemberTotal[m] be paid/committed.
- Partial/failed payments: Cart remains Locked; members can retry. Quotes remain valid until tip/coupon changes or expiration.

## Coupons & Discounts

- Item-level coupons (e.g., 20% off specific items) reduce ItemSubtotal[m] at the line level.
- Cart-wide coupons (e.g., $10 off cart, free delivery) produce DiscountCart (or reduce DeliveryFee) and are split evenly among participants.
- Usage limits: Enforced at conversion (or lock) atomically. If host’s coupon has per-user limits, business decision: either allow sharing the benefit evenly (recommended here) while usage is attributed to host, or restrict to host-only benefit (not this model).

## Transparency & Client Contracts

Add these fields to `TeamCartViewModel.Members[]` when Locked:
- QuotedAmount: final per-member amount (minor units and formatted)
- QuotedShares: object with Fees, Tip, Tax, Discount shares
- QuoteVersion and QuoteIsFinal (true while cart is locked and unchanged)

Cart-level flags:
- TotalsAreEstimated (false when Locked and quote present)
- TaxPolicy: descriptor of taxable base for client display

## Operational Considerations

- Idempotency: Store QuoteVersion and a hash of inputs; re-issuing the same quote yields identical per-member totals.
- Expiration: Lock has a TTL; quotes expire with the cart; late webhooks should be accepted but not mutate if the cart is expired or converted (still mark processed).
- Auditability: Persist per-member splits and rounding remainders for reconciliation.

## Implementation Plan (detailed)

The steps below follow the current architecture (Domain → Application → Infrastructure/Web) and reuse the outbox/inbox event pattern and policy-based authorization.

### 1) Domain Layer

- [ ] Add Domain Service `TeamCartQuoteService` (in `src/Domain/Services/`)
  - Responsibilities:
    - Compute authoritative cart quote and per-member splits using the Even Split algorithm and deterministic rounding.
    - Accept inputs: members with item subtotals (after item-level discounts), fees (delivery/service/other), tip (amount or percent), cart-wide discounts, tax rate and base policy.
    - Produce: `CartQuote` (GrandTotal, FeesTotal, TipAmount, TaxAmount, DiscountCart, Version, PerMember[] with ItemSubtotal, Shares, TotalDue, RemainderAssignments order).
  - Expose a pure function API for idempotency and testability.

- [ ] Extend Aggregate `TeamCart` to store quote snapshot
  - Add value objects/entities:
    - `MemberQuote` (UserId, ItemSubtotal, FeeShare, TipShare, TaxShare, DiscountShare, TotalDue)
    - `CartQuoteSnapshot` (Version, GrandTotal, SubtotalAfterItemDiscounts, FeesTotal, TipAmount, TaxAmount, DiscountCart, PerMember[], CreatedAt) inherits from `ValueObject`
  - Add methods:
    - `QuoteFinancials(TeamCartQuoteService, QuoteInput)` — persists `CartQuoteSnapshot`, raises `TeamCartQuoted`
    - `InvalidateQuote()` — clears snapshot (or bumps version), raises `TeamCartQuoteInvalidated`
    - `GetMemberQuote(UserId)` — returns `MemberQuote` (error if missing)
  - Locking rules:
    - On successful `LockForPayment`, immediately compute quote v1.
    - On `ApplyTip`/`ApplyCoupon`/`RemoveCoupon`, recompute quote and increment `Version`.

- [ ] New domain events
  - `TeamCartQuoted(TeamCartId, Version, GrandTotals, PerMember[])`
  - `TeamCartQuoteInvalidated(TeamCartId, PreviousVersion)` (optional if always re-quoting immediately)

### 2) Persistence (Domain/Infrastructure)

- [ ] Schema changes (EF Core migrations)
  - Option A (normalized): New table `TeamCartMemberQuotes`
    - Columns: TeamCartId (PK part), UserId (PK part), Version, ItemSubtotalCents, FeeShareCents, TipShareCents, TaxShareCents, DiscountShareCents, TotalDueCents, Created
    - Index: `IX_TeamCartMemberQuotes_TeamCartId_Version`
  - Option B (snapshot JSON on cart): Add JSON column `QuoteSnapshotJson` to `TeamCarts` for entire snapshot; simpler write, fewer rows. Recommended for v1 for simplicity.
  - Choose Option B initially to minimize scope; Option A can come later if query needs arise.

- [ ] Map snapshot in EF configuration for TeamCart

### 3) Application Layer

- [ ] LockTeamCartForPaymentCommandHandler
  - After `LockForPayment`, gather inputs (fees, tip, discount, tax policy) and call `QuoteFinancials`.
  - Persist cart; outbox will publish `TeamCartQuoted`.

- [ ] ApplyTipToTeamCartCommandHandler
  - After applying tip, call `QuoteFinancials` to recompute; bump version.
  - Persist cart.

- [ ] ApplyCouponToTeamCart / RemoveCouponFromTeamCart Handlers
  - After mutation, recompute quote; persist.

- [ ] InitiateMemberOnlinePaymentCommandHandler
  - Replace current amount computation (sum of member items) with quoted amount from `GetMemberQuote(currentUser)`.
  - Attach quote metadata to intent (e.g., `quote_version`, `quoted_cents`).

- [ ] HandleTeamCartStripeWebhookCommandHandler
  - For `payment_intent.succeeded` and `payment_intent.payment_failed`:
    - Parse metadata `{ teamcart_id, member_user_id, quote_version, quoted_cents }` if present.
    - Resolve the member’s current quote; validate intent amount equals `MemberQuote.TotalDue` for that version. If versions differ and cart was re-quoted, treat as out-of-date: acknowledge event but do not mutate, or map to failure requiring re-initiation (decision: acknowledge + no-op, mark processed).
    - Record success/failure via aggregate methods.

- [ ] ConvertTeamCartToOrderCommandHandler
  - Validation: ensure sum of `MemberPayments` equals snapshot `GrandTotal` for the latest version; if not, fail with actionable error (e.g., “Re-quote required”).
  - Use `CartQuoteSnapshot` values for delivery/tax/tip/discount to construct order financials (no recompute drift).

### 4) Real-time VM and Store (Redis)

- [ ] Extend `TeamCartViewModel`
  - Cart-level: `TotalsAreEstimated`, `TaxPolicy`, `QuoteVersion`, `DeliveryFee`, `TaxAmount`, `TipAmount`, `DiscountAmount`, `Total` (computed from quote components).
  - Member-level: `QuotedAmount`, `QuotedShares` { Fee, Tip, Tax, Discount }.

- [ ] Extend `ITeamCartStore` and `RedisTeamCartStore`
  - Methods to apply quote snapshot to VM: `ApplyQuoteAsync(cartId, snapshot)` and internal `RecalculateTotals` to include delivery/tax.

- [ ] Event Handlers (outbox → VM)
  - `TeamCartQuotedEventHandler`: writes full quote snapshot into Redis VM, broadcasts `ReceiveCartUpdated`.
  - Reuse existing handlers for tip/coupon updates to trigger new quote handling as needed.

### 5) Web API / Endpoints

- [ ] Optional: Quote endpoint `GET /api/v1/team-carts/{id}/quote`
  - Returns latest authoritative quote snapshot for display (same info as VM). Useful for non-RT clients.
  - Otherwise, rely on `/rt` to deliver quote via VM.

### 6) Configuration & Policies

- [ ] Add `TaxPolicyOptions` (tax base flags) and `DefaultTaxRate` (per restaurant/tenant) consumed by `TeamCartQuoteService`.
- [ ] Feature flags: allow toggling of “even-split financials v1”.

### 7) Tests

- Unit Tests (Domain/Service)
  - [ ] TeamCartQuoteService split/rounding: various n, remainders, extreme discounts, negative floors.
  - [ ] Determinism: identical inputs → identical outputs and remainder distribution by stable order.

- Application Functional Tests
  - [ ] Lock → quote produced, VM updated with QuoteVersion and per-member amounts.
  - [ ] Initiate online payment uses quoted amount; webhook validates amount.
  - [ ] Tip change after lock triggers re-quote; old intents succeed as no-op (ack-only) and require re-initiation.
  - [ ] Coupon apply/remove re-quote flow; conversion blocked until payments match latest quote.
  - [ ] Mixed payments (online + COD) cover grand total exactly; conversion succeeds.

- Web/Contract Tests
  - [ ] (If exposed) Quote endpoint returns snapshot matching VM.

### 8) Migration & Rollout

- [ ] DB migration (add `QuoteSnapshotJson` column on `TeamCarts`).
- [ ] Backfill: for existing Locked carts without quotes, compute quote on first mutation or background job.
- [ ] Observability: add metrics for quote version churn, amount mismatches, webhook out-of-date events, Redis quote apply failures.

### 9) Developer Notes

- Keep the quote computation pure and isolated (no external I/O or mutable state) for easy testing and idempotency.
- Treat quote version as a concurrency token for payment flows; include in payment intent metadata.
- Avoid recomputing delivery/tax in conversion; always read from the persisted quote snapshot to ensure consistency.

## Example (USD)

- Members: A adds $12.30 items; B adds $7.70 items → SubtotalCart = $20.00
- Delivery = $2.99; Service = $1.00; Tip 10% of SubtotalAfterItemDiscounts = $2.00; Tax 8% over (SubtotalAfterItemDiscounts + Fees + Tip):
  - FeesTotal = $3.99; TaxableBase = $20.00 + $3.99 + $2.00 = $25.99 → Tax = $2.08 (rounded)
  - GrandTotal = $20.00 + $3.99 + $2.00 + $2.08 = $28.07
- Even split shares (n=2):
  - Fees share: $1.99 and $2.00 (largest remainder to A by stable tie-break)
  - Tip share: $1.00 each
  - Tax share: $1.04 and $1.04 (remainder handled deterministically)
- Member totals:
  - A: 12.30 + 1.99 + 1.00 + 1.04 = 16.33
  - B: 7.70 + 2.00 + 1.00 + 1.04 = 11.74
  - Sum = $28.07 (matches GrandTotal)

---

This even-split model keeps the experience simple and fair: everyone pays their own items plus an equal share of the common costs and benefits, with precise rounding and airtight reconciliation.

