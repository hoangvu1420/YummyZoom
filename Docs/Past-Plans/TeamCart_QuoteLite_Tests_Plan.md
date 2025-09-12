# TeamCart Quote Lite — Detailed Test Plan

This plan outlines the unit and functional tests for the Quote Lite implementation, using existing patterns and guidelines in the repo.

## References

- Guidelines: Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md
- Existing TeamCart tests: tests/Application.FunctionalTests/Features/TeamCarts/**
- Stripe helpers: tests/Application.FunctionalTests/Features/Orders/PaymentIntegration/PaymentTestHelper.cs, TestConfiguration.Payment

## Unit Tests (Domain/Application)

1) ComputeQuoteLite_SplitsEvenly_AndPreservesSum
- Arrange: build cart state with 2–3 participants and item subtotals
- Act: ComputeQuoteLite with fees/tip/tax/discount
- Assert: sum(MemberTotals) == GrandTotal; remainder distributed deterministically by UserId

2) ComputeQuoteLite_ExcludesZeroItemMembers
- Arrange: includes a member with 0 subtotal
- Assert: not present in MemberTotals

3) ComputeQuoteLite_DiscountCappedAndNonNegativeGrandTotal
- Arrange: discount > subtotal base
- Assert: discount capped to base; GrandTotal >= 0

4) ComputeQuoteLite_VersionIncrements
- Call ComputeQuoteLite twice; Assert QuoteVersion increments

5) GetMemberQuote_BeforeAndAfterLock
- Assert failure before quote; success after ComputeQuoteLite

## Functional Tests (Application.FunctionalTests)

A) InitiateMemberOnlinePayment_QuoteLiteTests
- Initiate_UsesQuotedAmount_And_SetsMetadata()
  - Arrange: Create cart; add items; lock (triggers quote). Drain outbox.
  - Act: initiate payment for a member
  - Assert: response OK; metadata includes teamcart_id, member_user_id, quote_version, quoted_cents; amount equals quoted total

B) TeamCartWebhookQuoteValidationTests
- WebhookSucceeded_WithMatchingQuote_RecordsSuccess()
- WebhookSucceeded_WithStaleQuoteVersion_IsNoOp_Acked()
- WebhookSucceeded_WithQuotedCentsMismatch_ReturnsValidationError()
  - Arrange: as above; initiate; for stale: change tip to re-quote; send old webhook payload; assert processed with no mutation; for mismatch: alter quoted_cents

C) TeamCartQuoteRecomputeFlowTests
- TipChanged_Requotes_IncrementsVersion_RequiresReinit()
- CouponApply_Remove_Requotes_And_BlocksConversion_UntilPaid()
  - Arrange: lock + initiate; change tip/coupon; Assert VM updates and QuoteVersion++
  - Webhook with old version: ack no-op; re-init with new version and succeed; conversion passes only after all members paid/committed to new quote

D) ConvertTeamCartToOrder_QuoteLiteTests
- MixedPayments_SumEqualsGrandTotal_Converts()
  - Arrange: some members pay online; host commits COD; ensure sum == GrandTotal
  - Assert: conversion succeeds
- SumNotEqualToGrandTotal_FailsConversion()
  - Arrange: trigger mismatch scenario (e.g., stale intent processed as no-op)
  - Assert: conversion fails with actionable error

E) TeamCartRealTimeVmQuoteTests
- Lock_SetsQuoteInVm()
- TipOrCouponChange_UpdatesQuoteInVm()
  - Use GetTeamCartRealTimeViewModelQuery; assert QuoteVersion and Members[].QuotedAmount set/updated

## Test Mechanics & Conventions

- Base class: BaseTestFixture
- Use Testing.DrainOutboxAsync() after actions that raise events and/or update VM
- Stripe-backed tests: [Category("StripeIntegration")], [NonParallelizable]; guard via Stripe options presence
- Keep asserts deterministic: use stable UserId generation order when checking remainder distribution
- Avoid DB updates in this phase; rely on in-memory/transactional EF and Redis as configured for tests

## File/Namespace Suggestions

- tests/Application.FunctionalTests/Features/TeamCarts/Payments/InitiateMemberOnlinePayment_QuoteLiteTests.cs
- tests/Application.FunctionalTests/Features/TeamCarts/Payments/TeamCartWebhookQuoteValidationTests.cs
- tests/Application.FunctionalTests/Features/TeamCarts/Payments/TeamCartQuoteRecomputeFlowTests.cs
- tests/Application.FunctionalTests/Features/TeamCarts/Conversion/ConvertTeamCartToOrder_QuoteLiteTests.cs
- tests/Application.FunctionalTests/Features/TeamCarts/Realtime/TeamCartRealTimeVmQuoteTests.cs

---

This plan follows existing patterns and gives clear targets for verifying correctness, resilience, and UX signals of the Quote Lite implementation.

