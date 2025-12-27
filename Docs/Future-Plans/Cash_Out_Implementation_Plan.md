# Cash Out Flow Implementation Plan

## Scope

Implements the cash-out flow as defined in `Docs/Future-Plans/Cash_Out_Flow_Design.md`, using a mock payout provider for demo.

## Phase 0: Discovery and Alignment

1. Review existing payout/account domain code and read models.
2. Weekly payout cadence: allow one payout request every 7 days per restaurant, evaluated in UTC based on the last completed payout timestamp (or last requested if none completed yet).
3. Mock provider behavior: default to success after a short async delay (e.g., 5-10 seconds), with an optional configuration flag to force failure for testing.

## Phase 1: Domain + Persistence (Completed)

1. Add `Payout` aggregate/entity with fields:
   - `PayoutId`, `RestaurantAccountId`, `RestaurantId`, `Amount`, `Status`
   - `RequestedAt`, `CompletedAt`, `FailedAt`
   - `ProviderReferenceId`, `FailureReason`, `IdempotencyKey`
2. Add `PendingPayoutTotal` (Money) to `RestaurantAccount`.
3. Add behaviors:
   - `ReservePayout(amount)`
   - `ReleasePayoutHold(amount)`
   - keep `SettlePayout(amount)` unchanged
4. Add domain events:
   - `PayoutRequested`, `PayoutProcessing`, `PayoutCompleted`, `PayoutFailed`
5. Add EF Core configuration + migrations for:
   - `Payouts` table
   - `RestaurantAccounts.PendingPayoutTotal`
   - indexes (restaurant id + status + created time)

## Phase 2: Application Layer (Commands/Queries) (Completed)

1. Commands:
   - `RequestPayoutCommand`
   - `CompletePayoutCommand` (internal)
   - `FailPayoutCommand` (internal)
2. Query DTOs:
   - `PayoutEligibilityDto`
   - `PayoutDto`, `PayoutDetailsDto`
3. Queries:
   - `GetPayoutEligibilityQuery`
   - `ListPayoutsQuery`
   - `GetPayoutDetailsQuery`
4. Validation + authorization:
   - Owner-only for `RequestPayoutCommand`
   - Weekly cadence guard in eligibility + command handler
   - Check payout method exists
5. Idempotency:
   - Ensure repeated `RequestPayoutCommand` does not duplicate payout
   - Use `PayoutId`/client key + unique constraint

## Phase 3: Mock Payout Provider Integration

1. Create `IPayoutProvider` interface:
   - `RequestPayoutAsync(payoutId, amount, destination, idempotencyKey)`
2. Implement `MockPayoutProvider`:
   - Emits `Processing` then `Completed` after delay
   - Optional config toggle to fail
3. Wire mock provider into payout processing handler:
   - handler listens to `PayoutRequested`
   - calls mock provider and triggers internal commands

## Phase 4: Event Handlers + Projections

1. Handlers for:
   - `PayoutRequested` → move to `Processing`, invoke mock provider
   - `PayoutCompleted` → `CompletePayoutCommand`
   - `PayoutFailed` → `FailPayoutCommand`
2. Account transaction creation:
   - `PayoutSettled` → `AccountTransaction` (`PayoutSettlement`)
3. Read model updates:
   - `RestaurantAccountBalanceSnapshot`
   - payout history view / `PayoutQueue` (if used)

## Phase 5: API Surface (Completed)

1. Owner endpoints:
   - `GET /restaurants/{restaurantId}/account/payout-eligibility`
   - `POST /restaurants/{restaurantId}/account/payouts`
   - `GET /restaurants/{restaurantId}/account/payouts`
   - `GET /restaurants/{restaurantId}/account/payouts/{payoutId}`
2. Internal endpoints (optional for mock):
   - `POST /internal/payouts/{payoutId}/complete`
   - `POST /internal/payouts/{payoutId}/fail`

## Phase 6: UI (Demo Scope)

1. Payout screen:
   - balance/eligible amount
   - request payout button
   - last payout status
2. Payout history list (basic)
3. Status detail modal (optional)

## Phase 7: Testing + Demo Script

1. Unit tests:
   - `RestaurantAccount` hold/settle behavior
   - payout status transitions
2. Integration tests:
   - request payout → mock processing → completion
3. Demo checklist:
   - create restaurant owner
   - request payout
   - watch status move to completed
   - verify balance/ledger updates

## Suggested Execution Order

1. Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7
