# TeamCart Functional Spec — Current Implementation

This document describes the TeamCart functionality as implemented today: lifecycle, business rules, commands, Quote Lite financials, online/COD payments, conversion to Order, realtime behaviors, authorization, persistence, and testing conventions.

## 1) Overview & Lifecycle

- Actors:
  - Host: creates cart, locks for payment, manages tip/coupon, converts to order.
  - Members: add items, pay online or commit COD once locked.
- Lifecycle statuses: Open → Locked → ReadyToConfirm → Converted (or Expired)
- Source of truth:
  - Domain aggregate persisted in SQL (cart, items, members, payments, quote snapshot).
  - Realtime VM in Redis for low-latency collaboration (members/items/financial snapshot).

## 2) Roles & Authorization (Application policies)

- CreateTeamCart: authenticated user (customer); becomes Host.
- Add/Update/RemoveItem: TeamCartMember policy (host or guest member).
- Lock/ApplyTip/ApplyCoupon/RemoveCoupon/Convert: MustBeTeamCartHost.
- InitiateMemberOnlinePayment/CommitToCodPayment: MustBeTeamCartMember.
- Realtime Hub (SignalR): MustBeTeamCartMember to subscribe to `teamcart:{id}`.

## 3) Aggregate model (TeamCart)

- Members: host + guests (TeamCartMember) with UserId, Name, Role.
- Items: TeamCartItem with owning AddedByUserId, name, price snapshot, quantity, customizations.
- MemberPayments: commitments/transactions per user: method (Online, COD), status (Pending/CommittedToCOD/PaidOnline/Failed), amount, ref (for online).
- Financials (Quote Lite):
  - QuoteVersion (long), GrandTotal (Money), MemberTotals (UserId→Money) for latest quote.
  - Stored in SQL (columns + jsonb list) and applied to Redis VM (QuoteVersion, Members[].QuotedAmount).

## 4) Status transitions & domain events (selected)

- LockForPayment (Host, Open → Locked) → TeamCartLockedForPayment domain event.
- MemberCommittedToPayment (COD) or OnlinePaymentSucceeded (Online) recorded; when all members have completed their payment path, aggregate transitions to ReadyToConfirm and raises TeamCartReadyForConfirmation.
- Conversion marks cart Converted; expiration marks cart Expired.

## 5) Commands & Business Rules

- CreateTeamCart
  - Validates host name, deadline; creates share token; default deadline ~24h.
- AddItemToTeamCart / UpdateTeamCartItemQuantity / RemoveItemFromTeamCart
  - Validates membership, restaurant consistency, availability, customizations (min/max, group applies-to-item), duplicates, quantity > 0.
  - Updates SQL aggregate; realtime VM updated via outbox handlers for items.
- LockTeamCartForPayment (Host)
  - Requires non-empty cart, Open status; sets status Locked.
  - Computes Quote Lite (see section 6) and persists; updates Redis VM (QuoteVersion, QuotedAmount).
- ApplyTipToTeamCart (Host)
  - Locked only; persists tip; recomputes Quote Lite; updates Redis VM.
- ApplyCouponToTeamCart / RemoveCouponFromTeamCart (Host)
  - Locked only; validates coupon existence, window, limits (final limits also enforced on conversion); recomputes Quote Lite; updates Redis VM.
- InitiateMemberOnlinePayment (Member)
  - Locked only; amount = quoted per-member total (GetMemberQuote current user); creates PaymentIntent with metadata: source=teamcart, teamcart_id, member_user_id, quote_version, quoted_cents.
- CommitToCodPayment (Member)
  - Locked only; amount = quoted per-member total (fallback to items subtotal if no quote yet).

## 6) Quote Lite (Even Split, Versioned)

- When: computed at Lock, and on tip/coupon add/remove.
- Participants: members with item subtotal > 0.
- Components (MVP):
  - SubtotalAfterItemDiscounts = sum of members’ item subtotals (line-level discounts already applied).
  - FeesTotal (placeholder 2.99 in handlers), TipAmount (cart), TaxAmount (placeholder 0), DiscountCart (coupon calc per OrderFinancialService, capped to base).
  - GrandTotal = Subtotal + Fees + Tip + Tax − Discount (never negative after capping).
- Split:
  - Shared pot = Fees + Tip + Tax − Discount. If negative (discount-heavy), subtract from members.
  - Deterministic and sum-preserving minor-units split by stable UserId order.
  - MemberTotals[user] = items[user] ± share; stored as QuoteVersion+GrandTotal+MemberTotals.
- Validation:
  - Payment (Online/COD) amounts must match quoted per-member totals when QuoteVersion > 0; otherwise items subtotal.
  - Webhook: if quote_version != current, acknowledge no-op; if quoted_cents mismatch, return validation error.
  - Conversion: if QuoteVersion > 0, sum(MemberPayments) must equal GrandTotal.

## 7) Online Payments (Stripe)

- Initiation: per-member; PaymentIntent metadata set as above.
- Webhooks: `payment_intent.succeeded` / `payment_intent.payment_failed`
  - Constructed and validated via `IPaymentGatewayService`.
  - Idempotency via ProcessedWebhookEvents table.
  - If metadata lacks teamcart context → ack success (no-op).
  - Stale version → ack success (no-op).
  - On success with matching quote: record OnlinePaymentSucceeded and update aggregate; update Redis VM via event handler.
- Testing: use Stripe test SDK + helpers; guard runs if secrets absent.

## 8) Conversion to Order

- Preconditions:
  - Status ReadyToConfirm (all online payments PaidOnline + any COD commitments present).
  - With Quote Lite active (QuoteVersion > 0): sum(MemberPayments) == GrandTotal.
- Steps:
  - Validates address; loads coupon if any; enforces coupon usage limits; builds conversion via TeamCartConversionService and OrderFinancialService.
  - Persists Order; marks cart Converted; deletes/updates RT VM via handlers.

## 9) Realtime (Redis VM)

- View model fields (selected): Status, Members (PaymentStatus, CommittedAmount, OnlineTransactionId, QuotedAmount), Subtotal/Tip/Discount/Total, QuoteVersion.
- Store operations: create on TeamCartCreated, SetLocked/ApplyTip/Apply/RemoveCoupon/Item mutations, RecordOnlinePayment/Failure, CommitCod, UpdateQuote.
- UpdateQuoteAsync(cartId, quoteVersion, memberQuotedAmounts): sets QuoteVersion and members’ QuotedAmount.

## 10) Web (Endpoints & Hub)

- Endpoints: `/api/v1/team-carts/*` covers all commands/queries, including `/payments/online`, `/payments/cod`, `/convert`, and `/rt` for Redis VM.
- Feature flags: TeamCart enabled and RealTimeReady checked (Redis connection configured) to accept requests.
- SignalR Hub: `/hubs/teamcart`, requires authenticated user and MustBeTeamCartMember to subscribe; broadcast events via `ITeamCartRealtimeNotifier`.

## 11) Persistence (EF Core)

- TeamCart aggregate:
  - QuoteVersion: bigint required.
  - GrandTotal: owned Money → `GrandTotal_Amount` (decimal(18,2)), `GrandTotal_Currency` (char/varchar(3)).
  - MemberTotalsRows: jsonb list via reusable HasJsonbListConversion; maps to/from in-aggregate dictionary.
  - Items/Members/Payments mapped as owned collections; customizations stored as jsonb on items.
- Migrations: added to introduce the new columns; DB update is environment-dependent (in dev/test, applied by harness/migrator as configured).

## 12) Errors & Client Expectations (selected)

- TeamCart.NotFound, CanOnlyApplyFinancialsToLockedCart, OnlyHostCanModifyFinancials, CouponAlreadyApplied.
- Initiation/Webhook: InvalidPaymentAmount when metadata amount doesn’t match quote; stale quote version is acknowledged but ignored (client must re-initiate).
- Conversion: InvalidStatusForConversion or failure if paid sum != GrandTotal.

## 13) Testing & Conventions

- Unit tests: validate aggregate invariants and Quote Lite splitting + versioning.
- Functional tests (Application.FunctionalTests): end-to-end flows, Stripe-backed online payments, webhook validation, conversion success/failure conditions.
- Use `Testing.DrainOutboxAsync()` between steps involving domain events and VM updates.
- Stripe tests guarded with `[Category("StripeIntegration")]` and skipped if secrets missing.

## 14) Operational & Observability

- Logging: structured logs in handlers with CartId, UserId, operation name.
- Metrics (suggested): quote version churn, webhook stale/mismatch counts, conversion blocks due to mismatch, Redis CAS conflicts.
- Feature flags: TeamCart enablement; RealTimeReady based on Redis config.

## 15) Known MVP Simplifications

- FeesTotal and TaxAmount are placeholder values in handlers (2.99 and 0.00); can be replaced by service/policy-based calculators.
- VM quote updates executed directly from command handlers (could move to idempotent outbox event handlers).
- Quote snapshot persisted as aggregate columns + jsonb list, not a separate normalized table.

---

This spec reflects the current, working TeamCart with Quote Lite financials, suitable for client integration and continued iteration (e.g., richer fee/tax calculators, event-driven VM updates, and expanded test coverage).

