# Cash Out Flow Design (Restaurant Owner/Staff)

## Context

Existing domain pieces:

- `RestaurantAccount` aggregate manages `CurrentBalance` and payout method and emits events (`PayoutSettled`).
- `AccountTransaction` is the immutable audit record projected from account events.
- Order flow records revenue/fees via domain events (see `Docs/Feature-Discover/10-Order.md` and `Docs/Feature-Discover/8-RestaurantAccount.md`).
- Read models include `RestaurantAccountBalanceSnapshot` and `PayoutQueue` in `Docs/Architecture/Read_Models.md`.

We need a client + server flow for cash out that is consistent with the Order flow (event-driven, outbox/inbox, idempotent).

## Goals

- Clear UX for owners/staff: eligibility, payout setup, request, and status tracking.
- Server-side payout orchestration with idempotent external integration.
- Auditability: every balance change yields `AccountTransaction` rows.
- Consistent patterns with the Order flow (events, handlers, outbox/inbox).

## Actors

- Restaurant Owner: config payout method and request payout.
- Restaurant Staff: view balances/history only (no payout requests).
- System: payout processor, event handlers, webhook consumers.
- Admin/Finance: manual adjustments, payout troubleshooting.

## Proposed Client Flow

1. **Open Payouts screen**
   - Call `GetRestaurantAccountSummaryQuery` + `GetPayoutEligibilityQuery`.
   - Show `CurrentBalance`, `AvailableBalance`, last payout, payout method status.
2. **Setup payout method (if missing)**
   - Owner clicks "Add payout method" → `UpdatePayoutMethodCommand`.
   - Show masked destination after success.
3. **Request payout**
   - Owner selects amount (default max available), confirms destination.
   - Enforce weekly cadence (see eligibility below).
   - Submit → `RequestPayoutCommand`.
   - UI shows "Pending/Processing" with ETA and reference ID.
4. **Track payout**
   - History list from `ListPayoutsQuery` (or filter `AccountTransactions`).
   - Detail view shows status, failure reason, and related ledger entries.

## Server Integration Flow (Recommended)

### Summary

Introduce a payout request lifecycle. A payout is **requested** by the owner, then **processed** by the system. Balance is only deducted on completion, while a "hold" reserves funds during processing.

For demo purposes, use a **mock payout provider** that simulates async success/failure without external integrations.

### Steps

1. **RequestPayoutCommand (owner)**
   - Auth: owner role for `RestaurantId`.
   - Pre-checks: payout method exists; KYC/verification; weekly cadence; no account deletion.
   - Compute `AvailableBalance = CurrentBalance - PendingPayoutTotal`.
   - Reserve funds in `RestaurantAccount` (see domain change below).
   - Create `Payout` record with `Status = Requested`.
   - Emit `PayoutRequested` event (outbox).
2. **Payout processor**
   - Consumes `PayoutRequested` event (inbox + idempotency).
   - Calls **mock payout provider** with idempotency key = `PayoutId`.
   - Mock behavior: transition to `Processing`, then after a short delay emit `PayoutCompleted` (success) or `PayoutFailed` (configurable).
3. **Complete payout (system)**
   - Internal command `CompletePayoutCommand` loads payout + account.
   - Apply `account.SettlePayout(amount)` and release hold.
   - Emit `PayoutSettled` domain event for ledger creation.
4. **Fail payout (system)**
   - Internal command `FailPayoutCommand` releases the hold and marks payout failed with reason.
   - No balance change; keep audit trail in `Payout` history.
5. **Projection**
   - `PayoutSettled` → create `AccountTransaction` (`PayoutSettlement`, negative).
   - Update `PayoutQueue`, `RestaurantAccountBalanceSnapshot`, and payout history views.

This mirrors Order flow patterns: a user command creates an aggregate/event; system handlers perform external I/O; completion events materialize audit rows.

## Domain Changes (Recommended)

### 1) New `Payout` Aggregate (or Entity)

Minimal fields:

- `PayoutId`
- `RestaurantAccountId` / `RestaurantId`
- `Amount` (Money)
- `Status` (Requested, Processing, Completed, Failed, Canceled)
- `RequestedAt`, `CompletedAt`, `FailedAt`
- `ProviderReferenceId` (optional)
- `FailureReason` (optional)
- `IdempotencyKey` (string, default = `PayoutId`)

### 2) `RestaurantAccount` Enhancements

Add a "hold" concept to prevent double-spend while a payout is processing.

- `PendingPayoutTotal` (Money) or `HeldBalance` (Money)
- `AvailableBalance = CurrentBalance - PendingPayoutTotal`

New behaviors:

- `ReservePayout(amount)` → increases `PendingPayoutTotal`
- `ReleasePayoutHold(amount)` → decreases `PendingPayoutTotal`
- `SettlePayout(amount)` stays as-is, invoked on completion

Invariants:

- `ReservePayout(amount)` requires `amount > 0` and `amount <= AvailableBalance`.
- `ReleasePayoutHold` cannot drop below zero.

### 3) Domain Events

- `PayoutRequested(PayoutId, RestaurantAccountId, Amount)`
- `PayoutProcessing(PayoutId, ProviderReferenceId?)`
- `PayoutCompleted(PayoutId)`
- `PayoutFailed(PayoutId, Reason)`

`PayoutSettled` remains the balance event that creates `AccountTransaction`.

## Application Layer Commands/Queries

### Commands

- `RequestPayoutCommand(RestaurantId, Amount?)` → `PayoutRequestedResponse(PayoutId, Status)`
- `UpdatePayoutMethodCommand(RestaurantId, PayoutMethodDetailsDto)`
- `CompletePayoutCommand(PayoutId, ProviderReferenceId)` (internal)
- `FailPayoutCommand(PayoutId, Reason)` (internal)

### Queries

- `GetPayoutEligibilityQuery(RestaurantId)` → `PayoutEligibilityDto`
- `ListPayoutsQuery(RestaurantId, Status?, DateRange?, Page)` → `PaginatedList<PayoutDto>`
- `GetPayoutDetailsQuery(PayoutId)` → `PayoutDetailsDto`

## API Surface (Draft)

Owner-facing:

- `GET /restaurants/{restaurantId}/account/summary`
- `GET /restaurants/{restaurantId}/account/payout-eligibility`
- `PUT /restaurants/{restaurantId}/account/payout-method`
- `POST /restaurants/{restaurantId}/account/payouts` (request)
- `GET /restaurants/{restaurantId}/account/payouts`
- `GET /restaurants/{restaurantId}/account/payouts/{payoutId}`

System/internal:

- `POST /internal/payouts/{payoutId}/complete`
- `POST /internal/payouts/{payoutId}/fail`
- `POST /webhooks/payouts` (mock provider callbacks, optional)

## Consistency With Order Flow

- Event-driven orchestration (outbox/inbox) is identical to `OrderPaymentSucceeded` handling.
- Idempotency rules match: `PayoutId` as the stable idempotency key (similar to Stripe event IDs).
- Audit trail uses `AccountTransaction` and the same sign semantics.

## Edge Cases / Failure Handling

- **Duplicate request**: use idempotency key from client or server to avoid duplicate payout rows.
- **Concurrent requests**: guard with `PendingPayoutTotal` and optimistic concurrency on `RestaurantAccount`.
- **Provider timeouts**: treat as pending; reconcile via webhook or scheduled job.
- **Payout method changed mid-flight**: use the snapshot captured in the `Payout` record.
- **Refunds after payout request**: should reduce `AvailableBalance` but not exceed holds; if insufficient, block new payout requests.

## Clarifications for Open Questions

1. **Who can request payouts?**
   - **Answer:** Only the restaurant owner can request payouts. Staff are view-only.
2. **Minimum payout cadence?**
   - **Answer:** Weekly. Eligibility should block requests more frequently than weekly.
3. **Rolling reserves/holds (what does that mean)?**
   - **Explanation:** A rolling reserve is a small portion of recent revenue that stays locked for a period (e.g., 7-14 days) to cover refunds/disputes. It reduces available balance temporarily. If we need it, we would add a configurable reserve policy to the eligibility calculation (e.g., hold 5% of the last 14 days).
4. **When to settle?**
   - **Answer:** On completion only. Balance is deducted when the provider confirms success.
5. **Provider choice (what does this mean)?**
   - **Explanation:** The external payout provider (e.g., Stripe Connect, PayPal Payouts, bank ACH) determines how we initiate transfers and how/when we receive success/failure events (webhooks). This affects the exact webhook endpoints, payload handling, and idempotency keys.

## Policy Decisions (Applied)

- **Payout requester:** Owner only.
- **Cadence:** Weekly minimum between payout requests.
- **Settlement timing:** On completion (not at request time).

## Demo-Only Provider Notes

- Implement `IPayoutProvider` with a mock implementation.
- The mock should support:
  - configurable success/failure,
  - a short async delay to simulate processing,
  - idempotency by `PayoutId`.
- Use the same internal commands (`CompletePayoutCommand` / `FailPayoutCommand`) as the real provider would trigger.

## Next Steps

- Confirm policy decisions (open questions).
- Decide whether to introduce `Payout` aggregate or keep a simpler "request + immediate settle" flow.
- Update the domain model and feature discovery docs accordingly.
