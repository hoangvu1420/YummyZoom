# Coupon Domain — Design Review, Usage Analysis, and Recommendations

This document captures a deep analysis of the Coupon aggregate, its value objects, and how coupons are applied across Order and TeamCart flows. It evaluates alignment to business requirements and UX expectations, highlights gaps and risks, and proposes actionable improvements focused on domain business rules and application handlers.

## Executive Summary

- The Coupon aggregate and value objects (CouponValue, AppliesTo, CouponScope) are well-modeled and expressive. They enforce key invariants (code, validity window, min order, usage limits) and raise appropriate domain events.
- Discount calculation and validations are correctly moved into the OrderFinancialService, which cleanly handles applicability by scope (whole order, items, categories), min order, capping, and free-item logic.
- Application handlers implement a pragmatic, concurrency-safe strategy by enforcing usage limits in the repository with atomic SQL (both total and per-user), avoiding race conditions.
- TeamCart flow aligns with the TeamCart Functional Spec: host-only coupon application on a locked cart, pre-validation without usage increments, quote recomputation via domain event, and final usage enforcement at conversion.
- Key gaps: coupon code normalization on lookup, idempotency/when-to-increment policy for single-order flow, outbox usage for “coupon used” signals, and UX-friendly eligibility/feedback. Several edge cases and consistency checks can be tightened.

## Documents and Code Reviewed

- TeamCart functional spec: `Docs/Specs/TeamCart_Functional_Spec.md:1`
- Coupon aggregate docs: `Docs/Aggregate-Documents/11-Coupon-Aggregate.md:1`
- Order aggregate docs: `Docs/Aggregate-Documents/10-Order-Aggregate.md:1`
- Coupon aggregate: `src/Domain/CouponAggregate/Coupon.cs:1`
- AppliesTo: `src/Domain/CouponAggregate/ValueObjects/AppliesTo.cs:1`
- CouponScope: `src/Domain/CouponAggregate/ValueObjects/CouponScope.cs:1`
- CouponValue: `src/Domain/CouponAggregate/ValueObjects/CouponValue.cs:1`
- Coupon errors: `src/Domain/CouponAggregate/Errors/CouponErrors.cs:1`
- OrderFinancialService: `src/Domain/Services/OrderFinancialService.cs:1`
- Order.cs (applied coupon and invariants): `src/Domain/OrderAggregate/Order.cs:1`
- InitiateOrder handler (single-order flow): `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs:1`
- TeamCart Apply/Remove/Lock/Convert handlers:
  - `src/Application/TeamCarts/Commands/ApplyCouponToTeamCart/ApplyCouponToTeamCartCommandHandler.cs:1`
  - `src/Application/TeamCarts/Commands/RemoveCouponFromTeamCart/RemoveCouponFromTeamCartCommandHandler.cs:1`
  - `src/Application/TeamCarts/Commands/LockTeamCartForPayment/LockTeamCartForPaymentCommandHandler.cs:1`
  - `src/Application/TeamCarts/Commands/ConvertTeamCartToOrder/ConvertTeamCartToOrderCommandHandler.cs:1`
- Coupon repository: `src/Infrastructure/Persistence/Repositories/CouponRepository.cs:1`
- Fast-coupon-check plan: `Docs/Future-Plans/Fast-Coupon-Check_Strategy.md:1`

## Current Design Assessment

### Coupon Aggregate

- Fields cover: restaurant, code, description, value (percentage/fixed/free item), applies-to scope (whole/order, items, categories), validity window, min order, total usage limit, per-user usage limit, enabled/disabled, and soft-deletion.
- Invariants validated at creation: code presence/length, description presence, validity window ordering, min order amount (>0), usage limits (>0) when specified. Code normalized to uppercase at creation.
- Lifecycle methods: Use (in-memory increment + event), Enable/Disable (+events), UpdateDescription, Set/Remove MinOrder, Set/Remove Limits, IsValidForUse, AppliesToItem, MarkAsDeleted (+event). Note that Use() is documented for non-concurrent/test scenarios; repository handles atomic increments in production.
- Events: Created, Used, Enabled, Disabled, Deleted.

Observations:
- The aggregate keeps “authoritative” state (CurrentTotalUsageCount) but the production path bypasses in-aggregate mutation for concurrency safety (good). This leaves the aggregate’s in-memory count stale unless reloaded after repository-level increment.
- IsValidForUse() checks enabled/window/total-usage only; per-user usage is external by design (correct given concurrency concerns).

### Value Objects and Discount Calculation

- CouponValue supports three types with clear equality semantics and guardrails (percentage 1..100, fixed >0, free item by MenuItemId).
- AppliesTo enforces non-empty lists for specific items/categories, dedupes, and provides a clear AppliesToItem check and display text.
- OrderFinancialService centralizes calculations:
  - Validates enabled/window/min order.
  - Computes eligible base by scope.
  - Applies percentage/fixed/free-item logic and caps discount to eligible base.
  - Provides a variant for TeamCart items by mapping snapshots → OrderItem.

Observations:
- Free item logic chooses the lowest per-unit price among matched items (sensible default). If future promos allow “N free units” or “cheapest of X”, further policy hooks will be needed.

### Application Handlers and Repositories

Single-order flow (InitiateOrder):
- Fetch coupon by code, validate via OrderFinancialService, then atomically increment per-user and total counts (SQL upsert/conditional update). Uses the computed discount when creating the Order.
- Publishes a CouponUsed event constructed from previously loaded entity counts (may be stale vs DB). No outbox is used for this event.

TeamCart flow:
- ApplyCouponToTeamCart: host-only, Locked status required, resolves coupon by code, validates applicability now, sets coupon id on cart, and recomputes Quote Lite with discount via domain event (TeamCartQuoteUpdated). No usage increments here (per spec)—usage enforced at conversion.
- RemoveCouponFromTeamCart: host-only, Locked status, clears coupon, recomputes quote with zero discount.
- LockTeamCartForPayment: precomputes Quote Lite, optionally includes discount if coupon present.
- ConvertTeamCartToOrder: ReadyToConfirm only, recomputes discount from items, atomically increments per-user (host) and total counts, then converts to an Order via TeamCartConversionService and Order aggregate factories. Guards that paid sum equals GrandTotal (when quoted).

Repository (PostgreSQL dialect):
- `TryIncrementUsageCountAsync` uses conditional update; `TryIncrementUserUsageCountAsync` uses upsert with optional WHERE cap on usage.
- `GetByCodeAsync` compares code verbatim (case-sensitive behavior depends on collation). The domain normalizes codes to uppercase at creation, but lookups do not normalize input.

## Gaps, Risks, and UX Considerations

1) Case sensitivity and normalization on coupon lookup
- Risk: `GetByCodeAsync` uses equality on `Code == code`, while Coupon creation normalizes to uppercase. If clients send lowercase/mixed case, lookup may fail.
- UX: “Coupon not found” when a user typed a valid code in a different case.

2) When to increment usage (single-order flow)
- Current: per-user and total usage increments happen before payment succeeds (at InitiateOrder). If the online payment fails or the order is abandoned/cancelled, usage remains consumed.
- Business expectation: Many systems consume coupon usage only on successful payment or after a short reservation TTL.
- UX: Users may be unfairly blocked from reusing a coupon after a failed attempt.

3) Idempotency and signal consistency for “CouponUsed”
- The handler publishes a CouponUsed event constructed from a potentially stale in-memory count (aggregate was not mutated). There is no outbox for this event, risking lost updates.

4) Per-user usage attribution in TeamCart
- Current: conversion increments per-user usage for the host (who applied the coupon). This is defensible, but if policy later requires “per actual payer” or “per customer (not host)”, we will need a different attribution model.

5) Missing Coupon domain unit tests (coverage)
- There are robust tests for OrderFinancialService, but little direct testing of Coupon aggregate behaviors (enable/disable, limits mutations, Use(), applies-to equality, etc.).

6) Unclear policy for refunds/cancellations and coupon usage
- If an order is refunded or cancelled post-payment, should usage be restored? Currently no domain path reverses user/global usage counts.

7) Data and index hygiene for code uniqueness
- Ensure unique index per (RestaurantId, Upper(Code)). Current path implies uniqueness, but lookup/uniqueness at DB level should be case-insensitive.

8) Eligibility/feedback UX
- The stack already has granular CouponErrors (MinAmountNotMet, NotApplicable, UserUsageLimitExceeded). Surfacing these consistently in APIs/RT VM will help the client guide users (e.g., “Spend $7.50 more to use SAVE10”). The “Fast-Coupon-Check Strategy” doc outlines an excellent projection plan to enable this.

## Recommendations (Targeted Improvements)

1) Normalize lookups and enforce case-insensitive uniqueness
- In handlers, normalize input: `code = request.CouponCode.Trim().ToUpperInvariant()` before lookup.
- Alternatively (or in addition), adjust repository to normalize in query or use case-insensitive index:
  - Add unique index on `(RestaurantId, UPPER(Code))` and query by `UPPER(@code)`.
- Rationale: Prevents false negatives and enforces consistent semantics.

2) Shift usage increment timing for single-order flow
- Preferred: Move usage increments to payment success path (webhook) with idempotency keys bound to `order_id`.
- Alternative: Introduce a “reservation” row with TTL (e.g., a separate table CouponUsageReservations with metadata: user_id, coupon_id, order_id, expires_at). On success, finalize to CouponUserUsages and total; on failure/expiration, release.
- Ensure idempotency (ON CONFLICT DO NOTHING with unique (coupon_id, user_id, order_id)).
- Rationale: Aligns consumption with successful redemption, avoids unfair blocking, and tolerates retries.

3) Make CouponUsed signal outbox-driven and authoritative
- Emit “coupon used” via outbox after the authoritative increment (total and per-user) completes, sourcing counts from the DB (or projection) rather than the stale aggregate instance.
- Use this to feed read models like UserCouponEligibility.

4) Clarify TeamCart per-user usage policy (host vs members)
- If business wants “the host redeems the coupon,” keep as-is (increment host). If instead coupon is “per ordering user,” consider:
  - Counting per-user usage against the paying customer(s) derived from MemberPayments or the future Order’s customer.
  - Document policy in TeamCart spec to set clear expectations.

5) Add Coupon domain unit tests
- Cover: Create validations, Enable/Disable events, Set/Remove limits (including “cannot set below current usage”), MinOrder mutation, AppliesTo equality semantics, Use() non-concurrent path.

6) Define refund/cancellation policy for coupon usage
- If desired: add compensating decrement semantics on refund/cancellation events, guarded for idempotency and fraud controls (e.g., only within a window; not for partial refunds unless rules allow).
- Wire via outbox/event handlers from Order lifecycle.

7) Implement the “Fast Coupon Check” projections (incrementally)
- Projections:
  - ActiveCouponsByRestaurant (only potentially valid now)
  - ItemCouponMatrix (pre-fanned-out applies-to)
  - UserCouponEligibility (merged global + per-user eligibility)
- Command handlers stay authoritative for final application; projections power fast hints/ranking and reasons.

8) Tighten minor consistency details
- In TeamCart Lock/Apply/Remove, check currency coherence early; current code already uses tip currency as baseline for quote (ok).
- For free item coupons, consider verifying that the free item is included in the AppliesTo scope (or make scope irrelevant when Type=FreeItem and document it).
- Document “one coupon per order/cart” as an explicit rule (already enforced in TeamCart); ensure API rejects stacking.

## Alignment to Business Requirements and UX

- TeamCart requirements (spec) are satisfied:
  - Host-only coupon apply/remove while Locked.
  - Validation at apply-time without consuming usage; final enforcement at conversion.
  - Quote Lite recomputation and VM update via outbox event.
  - Conversion preconditions (status + paid sum equals grand total) prevent mismatches.
- Single-order flow validates applicability correctly and calculates discount accurately. The main UX gap is usage consumption timing for failed online payments.
- Error taxonomy is in place for client guidance; projections from Fast-Coupon-Check can materially improve the UX (eligibility lists, reasons, and ranking).

## Suggested Implementation Plan (In Scope for Domain/Handlers)

Phase 1 – Hardening and correctness
- Normalize coupon code input in handlers and/or repository.
- Move single-order usage increments to the payment-succeeded webhook, or add a reservation mechanism with TTL.
- Make “coupon used” outbox-driven after authoritative DB increments.
- Add unit tests for Coupon aggregate; top-up OrderFinancialService tests for free-item edge cases and rounding caps.

Phase 2 – Read models and UX
- Implement ActiveCouponsByRestaurant, ItemCouponMatrix, and UserCouponEligibility projections.
- Add an endpoint (or enrich existing query) to list “eligible coupons with reasons” for a cart snapshot using the fast strategy.

Phase 3 – Policy and lifecycle refinements
- Decide and document TeamCart per-user attribution policy.
- Decide on refund/cancellation effects on usage and implement compensating handlers as appropriate.

## Notable Code Touch Points

- Normalize input:
  - Orders: `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs:1`
  - TeamCart: `src/Application/TeamCarts/Commands/ApplyCouponToTeamCart/ApplyCouponToTeamCartCommandHandler.cs:1`
  - Repository (optional): `src/Infrastructure/Persistence/Repositories/CouponRepository.cs:1`

- Shift single-order usage increments to webhook handlers (not shown here), using `IPaymentGatewayService` metadata to correlate order and coupon.

- Outbox for coupon-used signal: introduce a notification handler that persists after DB increments and emits via outbox for projections.

## Closing

The Coupon domain and its usage across Order and TeamCart are largely sound and match the documented specs. Addressing the small-but-impactful gaps around lookup normalization, usage consumption timing, and outbox-driven signaling will improve correctness, scalability, and user experience. The proposed read models unlock a much faster and friendlier coupon experience without compromising authoritative checks in command handlers.

