# TeamCart Financials — Approach C: Quote Lite (Outline + Implementation Plan)

## Overview

Approach C delivers an “even‑split, quoted per‑member total” with minimal domain churn. It avoids a full quote object graph and keeps persistence simple while achieving the core UX goals: predictable totals, per‑member clarity, and validation of payments against a quoted amount and version.

## Goals (MVP)

- Evenly split all cart‑level adjustments (delivery, service fees, tip, tax, cart‑wide discount) across participating members.
- Compute an authoritative per‑member “QuotedAmount” after lock; re‑quote on tip/coupon changes.
- Validate online payment webhooks against the quoted amount and quote version.
- Block conversion unless paid/committed sum equals the quoted grand total.
- Small, targeted changes; easy to extend later to full quote details if needed.

## Non‑Goals (for now)

- Per‑member share breakdown (Fee/Tip/Tax/Discount) in the domain model (can be added later).
- Persisted full quote snapshot as a normalized structure (use a simple JSON blob or in‑aggregate dictionary).
- Complex tax base policy modeling (use current defaults or a single rate input).

## Data Model (Minimal)

- TeamCart (aggregate)
  - Add: `long QuoteVersion` (derived from persisted state; see logic below) — optional if embedded in MemberTotals blob.
  - Add: `Dictionary<UserId, Money> MemberTotals` (or equivalent map type) — quoted per‑member totals.
  - Add: `Money GrandTotal` — quoted cart total for latest version.
  - Persisting: store MemberTotals + GrandTotal in a JSON column or as serialized fields on TeamCart.

## Algorithm (Equal Split with Deterministic Rounding)

- Participants: members with ItemSubtotal > 0 at lock time.
- Compute SubtotalAfterItemDiscounts, FeesTotal, TipAmount, TaxAmount, DiscountCart using existing services/utilities (same as conversion inputs).
- GrandTotal = SubtotalAfterItemDiscounts + FeesTotal + TipAmount + TaxAmount − DiscountCart.
- Split evenly across participants in minor units (cents):
  - q = floor(GrandTotal_cents / n), r = GrandTotal_cents % n.
  - Assign q to each participant; assign +1 cent to the first r participants following a stable order (e.g., ascending UserId GUID).
- MemberTotals[m] = items[m] + equal share of (Fees+Tip+Tax−Discount) with rounding; sum preserved.

## Versioning

- QuoteVersion = (previous QuoteVersion) + 1 when re‑quoting; initialize to 1 on first lock.
- Persist version with the MemberTotals blob (either as a field or adjacent property).
- Include `quote_version` and `quoted_cents` in payment intent metadata.

## Domain Changes (Small, additive)

- TeamCart (new members):
  - Fields: `MemberTotals`, `GrandTotal`, `QuoteVersion`.
  - Method: `ComputeQuoteLite(memberItemSubtotals, fees, tip, tax, discount)`
    - Implements equal split algorithm.
    - Stores `MemberTotals`, `GrandTotal`, and increments `QuoteVersion`.
  - Method: `GetMemberQuote(UserId)` → returns quoted Money for the user.
  - Re‑quoting triggers: after `LockForPayment` (initial), after `ApplyTip`, `ApplyCoupon`, `RemoveCoupon`.
- Domain Events (optional for RT updates): `TeamCartQuotedLite(TeamCartId, QuoteVersion, GrandTotal)` — can be added later; for MVP, rely on handlers already updating VM on tip/coupon changes.

## Application Layer Wiring

1) LockTeamCartForPaymentCommandHandler
- After successful lock:
  - Gather item subtotals per member (existing projection over cart items), fees/tip/tax/discount (use OrderFinancialService or current calculators).
  - Call `ComputeQuoteLite(...)` on the aggregate.
  - Persist cart.

2) ApplyTipToTeamCartCommandHandler
- After tip applied:
  - Recompute quote (call `ComputeQuoteLite(...)`), persist.

3) ApplyCouponToTeamCart/RemoveCouponFromTeamCart
- After mutation:
  - Recompute quote, persist.

4) InitiateMemberOnlinePaymentCommandHandler
- Replace amount calculation with `GetMemberQuote(currentUser)`.
- Add metadata to PaymentIntent: `{ teamcart_id, member_user_id, quote_version, quoted_cents }`.

5) HandleTeamCartStripeWebhookCommandHandler
- On `payment_intent.succeeded` and `payment_intent.payment_failed`:
  - Parse metadata.
  - Validate amount equals current `MemberTotals[user]` for the specified quote version.
  - If version mismatch: acknowledge as processed but do not mutate (requires re‑initiation by client), or return a domain error to prompt retry (choose one policy; recommend ack + no‑op in MVP).
  - Record success/failure using existing payment methods (already implemented).

6) ConvertTeamCartToOrderCommandHandler
- Validate sum of successful online payments + COD commitments equals `GrandTotal` for latest `QuoteVersion`.
- If mismatch, return actionable error (e.g., “Quote changed — please re‑confirm payments”).
- Use quote inputs or recompute order financials consistently; for MVP, reuse current conversion calculators but ensure they align with the quoted grand total.

## Realtime VM (Minimal)

- Add to TeamCartViewModel:
  - `QuoteVersion` (long)
  - `Members[].QuotedAmount` (decimal/currency)
  - Optionally set `TotalsAreEstimated = false` once quoted.
- Update event handlers on lock/tip/coupon to refresh these fields (or add a `TeamCartQuotedLite` handler if created).

## Persistence Strategy (MVP)

- JSON snapshot on TeamCart:
  - `QuoteVersion`, `GrandTotal` (amount+currency), and `MemberTotals` (array of { userId, cents } or { userId, amount, currency }).
  - EF: Map to a string/json column; serialize/deserialize on save/load.
- No new tables or complex mapping for MVP.

## Testing Plan

- Unit tests (Domain/Application):
  - ComputeQuoteLite correctness:
    - 1, 2, 3+ participants; equal split; deterministic remainder distribution; large/small totals; zero‑item members excluded; single participant.
    - Discount capping to base; non‑negative GrandTotal; QuoteVersion increments on each call.
  - GetMemberQuote returns failure before quote; success after lock.

- Functional tests (Application.FunctionalTests) — follow Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md:
  - InitiateMemberOnlinePayment_QuoteLiteTests
    - Initiate_UsesQuotedAmount_And_SetsMetadata()
      Arrange: lock + quote; Act: initiate payment; Assert: intent created; metadata contains teamcart_id, member_user_id, quote_version, quoted_cents; amount matches quoted.

  - TeamCartWebhookQuoteValidationTests
    - WebhookSucceeded_WithMatchingQuote_RecordsSuccess()
    - WebhookSucceeded_WithStaleQuoteVersion_IsNoOp_Acked()
    - WebhookSucceeded_WithQuotedCentsMismatch_ReturnsValidationError()

  - TeamCartQuoteRecomputeFlowTests
    - TipChanged_Requotes_IncrementsVersion_RequiresReinit()
      Arrange: lock + quote + initiate; change tip; Act: webhook with old version; Assert: ack no‑op; re‑init with new version → success.
    - CouponApply_Remove_Requotes_And_BlocksConversion_UntilPaid()

  - ConvertTeamCartToOrder_QuoteLiteTests
    - MixedPayments_SumEqualsGrandTotal_Converts()
    - SumNotEqualToGrandTotal_FailsConversion()

  - TeamCartRealTimeVmQuoteTests
    - Lock_SetsQuoteInVm()
    - TipOrCouponChange_UpdatesQuoteInVm()
      Use GetTeamCartRealTimeViewModelQuery to assert vm.QuoteVersion and Members[].QuotedAmount updated.

Notes:
- BaseTestFixture; use Testing.DrainOutboxAsync() between steps.
- Stripe‑backed tests should respect TestConfiguration.Payment guard and categories (e.g., [Category("StripeIntegration")], [NonParallelizable]).

## Rollout & Observability

- Metrics: quote version churn, mismatched webhook amounts, conversion blocks due to quote mismatch.
- Feature flag: enable/disable “Quote Lite” behavior to allow quick rollback.

## Step‑by‑Step Implementation Checklist

A) Domain (TeamCart)
- [x] Add fields: `QuoteVersion`, `GrandTotal`, `MemberTotals` (map UserId→Money)
- [x] Add method: `ComputeQuoteLite(...)` (equal split of (Fees+Tip+Tax−Discount) + items)
- [x] Add method: `GetMemberQuote(UserId)`
- [x] Invoke `ComputeQuoteLite` after `LockForPayment`, `ApplyTip`, `ApplyCoupon`, `RemoveCoupon`

B) Application
- [x] Lock handler: compute quote and persist
- [x] Tip handler: recompute quote and persist
- [x] Coupon handlers: recompute quote and persist
- [x] Initiate payment: use `GetMemberQuote`; add metadata
- [x] Webhook: validate amount against quoted; handle stale version policy
- [x] Convert: require sum == `GrandTotal` for latest version

C) Realtime / Store
- [x] Extend TeamCartViewModel: `QuoteVersion`, `Members[].QuotedAmount`
- [x] Update existing VM via command handlers (`UpdateQuoteAsync`) (MVP)

D) Persistence
- [x] Map `QuoteVersion` (bigint), `GrandTotal` (owned Money), `MemberTotals` (jsonb list) in EF configuration
- [x] Add DB migration to create columns and deploy

E) Tests
- [x] Unit tests for split/rounding and re‑quote
- [x] Functional tests for lock→quote→pay→convert and re‑quote flows

---

This plan achieves even split, versioned quoted amounts, payment validation, and conversion consistency with small, targeted changes and a simple persistence model. It can be upgraded later to a full quote snapshot (Approach Full) without breaking client integrations.
