# TeamCart End-to-End Integration Tests – Outline and Recommendations

## Overview

This document proposes a focused set of end-to-end (E2E) functional tests for the TeamCart feature that exercise realistic, multi-user flows across commands, queries, domain events, Redis projections, payments (Stripe), and final conversion to Order. These tests build upon the existing unit/functional tests for individual commands, queries, and event handlers and follow the patterns in Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md.

Key goals:

- Validate real multi-member flows from creation → payment → conversion
- Use real infrastructure where possible: Testcontainers (Postgres, Redis) and Stripe SDK in test mode
- Assert event-driven side-effects deterministically via Outbox drain
- Verify correctness of financials (tip, coupon, member payments, final order transactions)
- Verify idempotency and resiliency (duplicate webhooks, partial payments)

## References

- Orders E2E payment tests: `tests/Application.FunctionalTests/Features/Orders/PaymentIntegration/OnlineOrderPaymentTests.cs`
- Orders lifecycle smoke: `tests/Application.FunctionalTests/Features/Orders/Commands/Lifecycle/FullLifecycleSmokeTests.cs`
- TeamCart functional tests (per-command): `tests/Application.FunctionalTests/Features/TeamCarts/*`
- Test guidelines: `Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md`

## Proposed Test Suite Layout

Add new suites under TeamCarts mirroring the Orders structure for readability:

- `tests/Application.FunctionalTests/Features/TeamCarts/PaymentIntegration/`
  - `TeamCartOnlinePaymentFlowTests.cs` (Stripe-backed, multi-member flows)
  - `TeamCartWebhookIdempotencyTests.cs` (duplicate/invalid webhook handling)
- `tests/Application.FunctionalTests/Features/TeamCarts/FullFlow/`
  - `TeamCartFullLifecycleSmokeTests.cs` (creation → lock → financials → payments → convert)
  - `TeamCartMixedPaymentsFlowTests.cs` (online + COD mixture)
  - `TeamCartCouponAndTipFlowTests.cs` (locked-only financials, coupon usage limits, final totals)
  - `TeamCartExpirationFlowTests.cs` (deadline/expiration interactions with payments and conversion)

Each class should inherit `BaseTestFixture` and use the existing helpers:

- `TeamCartTestBuilder` + `TeamCartTestScenario` for multi-user setup
- `TeamCartAuthorizationExtensions` to switch roles and attach permission claims
- `Testing.DrainOutboxAsync()` between steps involving domain events
- `PaymentIntegration/PaymentTestHelper` for Stripe payment confirmation + webhook payload/signature

## Core Flows and Scenarios

Below are the recommended end-to-end scenarios, with concrete steps and assertions.

### 1) All-Online Payment Flow (Happy Path)

Class: `TeamCartOnlinePaymentFlowTests`

Flow steps:

1. Arrange
   - Build scenario with one host and two guests for `DefaultRestaurantId` using `TeamCartTestBuilder`.
   - `DrainOutboxAsync()` to materialize initial Redis VM after `TeamCartCreated`.
2. Add Items (multi-user)
   - As Host: add 1–2 items (e.g., ClassicBurger). Drain outbox.
   - As Guest A and Guest B: add items. Drain outbox after each.
   - Assert real-time VM reflects members, items, subtotals.
3. Lock and Financials
   - As Host: `LockTeamCartForPayment`, then apply tip (e.g., 10%) and coupon (valid code). Drain outbox.
   - Assert: Apply is host-only and locked-only; VM updated to reflect tip and coupon.
4. Initiate Online Payments
   - For each member (Host, Guest A, Guest B): `InitiateMemberOnlinePaymentCommand` and capture `PaymentIntentId`.
   - Set `StripeConfiguration.ApiKey` from `IOptions<StripeOptions>` in `[SetUp]`.
5. Confirm Payments via Stripe SDK
   - For each intent, call `PaymentTestHelper.ConfirmPaymentAsync(..., VisaSuccess)`.
6. Process Webhooks
   - For each intent, generate webhook payload with metadata `{ source=teamcart, teamcart_id, member_user_id }` and sign it using `PaymentTestHelper.GenerateWebhookSignature`.
   - Send `HandleTeamCartStripeWebhookCommand` for each payload.
   - Drain outbox; assert domain events fired and VM payment statuses updated.
7. Ready to Confirm
   - Assert TeamCart status transitioned to `ReadyToConfirm` and all member payments are recorded with method Online and status PaidOnline.
8. Convert to Order
   - As Host, call `ConvertTeamCartToOrderCommand` with delivery address and special instructions.
   - Assert: Success; Order persisted; `Order.SourceTeamCartId` set; PaymentTransactions count equals members; types = CreditCard; TeamCart status = Converted.

Notable asserts:

- Coupon usage counters incremented via repository; final discount applied matches `OrderFinancialService` calculation
- `TeamCartReadyForConfirmation` raised once; VM broadcasts (optionally mock `ITeamCartRealtimeNotifier` and verify calls if needed)
- Outbox-driven VM matches domain state after each step

### 2) Mixed Payments (Online + COD)

Class: `TeamCartMixedPaymentsFlowTests`

Flow steps (as above) with differences:

- One member selects COD via `CommitToCodPaymentCommand`; others pay Online via Stripe.
- After all commitments/payments, TeamCart becomes `ReadyToConfirm` with `cashAmount > 0` in `TeamCartReadyForConfirmation`.
- Convert to Order; assert `PaymentTransactions` contains a mix: COD → `PaymentMethodType.CashOnDelivery`, Online → `PaymentMethodType.CreditCard`.
- Verify payment transaction totals align to final order total via conversion service’s proportional adjustment.

### 3) Webhook Idempotency and Resiliency

Class: `TeamCartWebhookIdempotencyTests`

Scenarios:

- Duplicate webhook delivery: send `payment_intent.succeeded` twice for same intent; assert single member payment and no duplicate events/VM drifts.
- Webhook missing teamcart metadata: ensure no-op and still marked processed.
- Invalid signature: `ConstructWebhookEvent` fails → command returns failure; nothing persisted.

### 4) Coupon and Tip Flow (Locked-only Financials)

Class: `TeamCartCouponAndTipFlowTests`

Scenarios:

- Apply coupon and tip only after `LockTeamCartForPayment` as Host; assert failures otherwise.
- Usage limits: combine two conversions by the same host using a coupon with `UserUsageLimit = 1` to assert the second conversion fails (pattern already proven in `ConvertTeamCartToOrderCommandTests`).
- Ensure real-time VM reflects coupon added/removed via outbox event handlers.

### 5) Expiration Flow

Class: `TeamCartExpirationFlowTests`

Scenarios:

- Create a cart with near-term deadline; do not complete payments; invoke `ExpireTeamCartsCommand(now, batchSize)`.
- Assert cart is `Expired`; conversion fails with `TeamCartErrors.InvalidStatus`.
- If webhook arrives post-expiration, handler recomputes and attempts to record payment; expect failure due to non-`Locked` status; event remains unprocessed (current handler behavior). Document/decide desired behavior; consider marking as processed with warning to avoid retries.

## Cross-Cutting Testing Pattern

- Always call `DrainOutboxAsync()` after commands that raise domain events and before asserting side-effects (projections, notifications).
- Use `TeamCartTestScenario.ActAsHost()/ActAsGuest(name)` to switch identities and attach proper `Roles.TeamCartHost/Member` claims.
- Assert both domain state (via repository `FindAsync`) and projection state (via `GetTeamCartRealTimeViewModelQuery`).
- For Stripe-backed tests, retrieve keys via `IOptions<StripeOptions>` and set `StripeConfiguration.ApiKey` in `[SetUp]`.

## Configuration and Execution

- Stripe test mode secrets are required for Stripe-backed flows:

  User Secrets (for `Application.FunctionalTests`):

  {
    "Stripe": {
      "SecretKey": "sk_test_...",
      "PublishableKey": "pk_test_...",
      "WebhookSecret": "whsec_..."
    }
  }

- Mark Stripe-backed tests with a category, e.g., `[Category("StripeIntegration")]`, and consider `[Explicit]` to avoid running by default in CI unless secrets are present.
- Add a lightweight guard to skip Stripe tests when keys are absent (e.g., `Assert.Inconclusive("Stripe not configured")`).

## Notable Considerations

- Determinism: Always drain the outbox; avoid `Task.Delay`/sleep.
- Parallelization: Stripe calls are slower and subject to rate limits; mark Stripe E2E tests as `[NonParallelizable]` or run them in a dedicated test collection.
- Idempotency: Ensure duplicate webhooks do not create duplicate member payments; verify `ProcessedWebhookEvents` persistence.
- Financial correctness:
  - Tip and coupon allowed only on locked carts by Host.
  - Conversion service adjusts member payments proportionally to match final order total (delivery, tax, tip, discount). Assert delta within tolerance.
  - When coupon applied, ensure usage counters are atomically incremented and failures roll back conversion.
- Real-time VM: Assert key VM fields after each step (status, members, items, totals, member payment statuses) to verify Redis projection handlers are wired.
- Expiration: Clarify expected behavior for late webhooks on expired carts; current handler acknowledges as success only when metadata is missing or ids invalid. Consider marking payment events as processed even when cart cannot be mutated to avoid retries.

## Recommended Infrastructure Enhancements

1. Stripe test guard:
   - Add a small helper (e.g., `StripeTestConfigGuard`) in `Application.FunctionalTests.Infrastructure` to fetch `StripeOptions`, set `StripeConfiguration.ApiKey`, and `Assert.Inconclusive` if missing.
   - Apply `[SetUp]` in Stripe-backed test classes to call the guard.

2. TeamCart payment helper:
   - Reuse `PaymentTestHelper` for webhook payload/signature; optionally add convenience helpers:
     - `GenerateTeamCartWebhookPayload(teamCartId, memberUserId, paymentIntentId, eventType)`
     - Thin wrappers around existing `GenerateWebhookPayload` to avoid test duplication.

3. Test categories and non-parallelizable markers:
   - Tag Stripe-backed classes with `[Category("StripeIntegration")]` and `[NonParallelizable]`.

4. Optional notifier verification:
   - In flows where broadcast correctness matters, replace `ITeamCartRealtimeNotifier` with a mock to verify `NotifyCartUpdated`, `NotifyPaymentEvent`, and `NotifyReadyToConfirm` calls after `DrainOutboxAsync()`.

## Concrete Test Stubs (names and intent)

- `TeamCartOnlinePaymentFlowTests`
  - `CreateAndPayAllMembers_WithStripe_SucceedsAndConverts()`
  - `CreateAndPayAllMembers_WithStripe_DuplicateWebhook_IsIdempotent()`

- `TeamCartMixedPaymentsFlowTests`
  - `MixedOnlineAndCOD_AllCommitted_ConvertsWithMixedPaymentTransactions()`

- `TeamCartCouponAndTipFlowTests`
  - `ApplyTipAndCoupon_AfterLock_AffectsTotals_AndConverts()`
  - `CouponUserUsageLimit_EnforcedAcrossMultipleConversions()`

- `TeamCartExpirationFlowTests`
  - `UnpaidCart_BeforeDeadline_ExpiresAndBlocksConversion()`
  - `ExpiredCart_ReceivingWebhook_ShouldNotChangeState_DecideProcessingPolicy()`

## Next Steps

- Implement the test classes under the proposed folders, starting with the All-Online flow.
- Add the Stripe test guard and categories; run locally with configured secrets.
- Iterate on projections and handler gaps discovered by E2E (e.g., webhook-on-expired policy) and document decisions.

