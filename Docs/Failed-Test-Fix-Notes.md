# Failed Test Fix Notes

## Summary
This document captures recurring issues seen in functional test failures, along with root causes, fixes applied, and notes to prevent regressions.

## Notable Issues
- Assertions relied on hardcoded amounts or counts that no longer match seeded data.
- Functional tests created orders with mismatched currencies (USD vs VND) leading to Money addition exceptions.
- MenuTestDataFactory defaults to USD when PriceCurrency is omitted, which can break order creation under VND pricing.
- Team cart COD payment tests assumed payments are allowed on Locked carts and that empty carts fail, but the domain now requires Finalized status and returns success for members with no items.
- Team cart conversion tests expected COD commits after Lock, but conversion requires ReadyToConfirm (FinalizePricing + member payments) before Convert.
- Team cart Stripe webhook tests initiated online payment after Lock, but initiation now requires Finalized status (Quote Lite).
- Team cart online payment initiation tests assumed Locked status was sufficient; initiation now requires Finalized status and uses quote-based totals.
- Team cart coupon-apply event tests can miss their VM update if seeded outbox backlog delays TeamCartCreated/CouponApplied events; drain may not reach the target event.
- Team cart full-flow tests committed COD after locking and applying tip/coupon, but COD commit now requires Finalized status.
- Team cart payment failure tests expected Locked status after failed webhook, but carts remain Finalized after pricing is finalized.
- Team cart expiration tests finalize pricing to initiate payment; ExpireTeamCarts only targets Open/Locked carts, so finalized carts are skipped and no expiration occurs.
- Stripe webhook tests assumed quoted_cents are always amount * 100; VND uses 0 decimals, so quoted_cents must be unscaled.
- Outbox tables can be prefilled from seeding, so short drains may miss newly enqueued events.
- Outbox drains can process many seeded order events, so notifier mocks may fire for unrelated orders.
- Order event handler tests can miss their target broadcast if seeded outbox entries are drained first.
- Order delivered revenue assertions relied on hardcoded totals, which broke with seeded balances.
- Order delivered broadcasts can fire multiple times for the same order during a drain, even though the handler is idempotent.
- Order query tests can fail when currency assertions are hardcoded to USD.
- EF Core `FindAsync` hit the multiple-collection include warning configured as an exception.
- `--no-build` runs used stale binaries and masked test updates.
- Idempotency assertions compared data outside the recompute window.

## Root Causes
- Test environments now seed production-like data (orders/users/restaurants), which inflates counts and alters totals.
- Pricing defaults are VND (StaticPricingService) while some tests assume USD values.
- Menu item scenarios created with implicit USD prices conflicted with VND delivery fees.
- Team cart payment workflow tightened to require Finalized carts, and CommitToCodPayment is a no-op success when a member has no items/quote.
- Conversion flow now validates ReadyToConfirm status; carts must be finalized and paid before Convert.
- Online payment initiation for team carts now requires Finalized status and available quotes.
- Online payment initiation tests must finalize pricing before initiating and expect `CanOnlyPayOnFinalizedCart` on open/locked carts.
- Stripe webhook tests must compute quoted_cents via CurrencyMinorUnitConverter (VND uses 0 decimals).
- Clear or filter OutboxMessages before draining in team cart event tests to ensure the VM update is processed.
- Finalize pricing before COD commits in full-flow tests that lock and apply tip/coupon.
- Expect Finalized status after failed payment webhooks when pricing has been finalized.
- For expiration tests that initiate payment, set status back to Locked (test-only) and force `ExpiresAt` in the past before running `ExpireTeamCarts`.
- Large outbox backlogs mean the target event may not be processed within the default drain timeout.
- EF Core configuration throws on `MultipleCollectionIncludeWarning` for certain `FindAsync` paths.
- Tests coupled to global totals instead of verifying the scenario’s own contributions.

## Solutions Applied
- Replace hardcoded totals with values derived from seeded entities (menu item prices, currency).
- Use default restaurant/menu items to keep currency aligned with VND pricing.
- Set PriceCurrency explicitly for MenuTestDataFactory items or normalize item currency before ordering.
- Finalize team cart pricing before COD commits and ensure the open-cart failure path includes member items; assert `CanOnlyPayOnFinalizedCart`.
- Finalize pricing before committing COD in conversion scenarios so the cart reaches ReadyToConfirm prior to Convert.
- Finalize pricing before initiating online payment in Stripe webhook tests.
- Finalize pricing before initiating online payment in team cart online payment tests and assert the finalized-only error code.
- Delete seeded OutboxMessages and filter to the target TeamCartId before draining when asserting coupon updates/notifications.
- Insert `FinalizePricingCommand` before `CommitToCodPaymentCommand` in full-flow tip/coupon conversion tests.
- Adjust payment-failure flow assertions to Finalized status after adding FinalizePricing.
- In expiration flow tests, manually update `Status`/`ExpiresAt` before calling `ExpireTeamCartsCommand` to ensure a candidate is found.
- Fetch specific fields with targeted queries instead of `FindAsync` to avoid include warnings.
- Scope idempotency checks to the recompute window buckets and compare those values only.
- Avoid `--no-build` when test edits are made.
- Clear or isolate OutboxMessages before asserting processed events to avoid backlog interference.
- Scope notifier and inbox/outbox assertions to the target order/event ID when seeded events are present.
- Delete seeded OutboxMessages before order event handler tests to ensure the drain reaches the test event.
- Compute revenue deltas from the persisted order total instead of hardcoded amounts.
- Assert delivered broadcasts at least once and scope by order/status instead of strict counts.
- Derive currency expectations from persisted order totals instead of hardcoded strings.

## Notes
- Prefer assertions that validate minimum expected contributions from the test setup, not absolute global totals.
- When prices or currencies are involved, read from the persisted MenuItem or pricing config.
- If MenuTestDataFactory is used, always set PriceCurrency to match StaticPricingService.
- If EF Core warnings are configured to throw, avoid query paths that implicitly include multiple collections.
- When asserting outbox processing, ensure the table is clean or the drain timeout is sufficient.
