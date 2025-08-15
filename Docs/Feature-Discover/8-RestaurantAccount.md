# Feature Discovery & Application Layer Design — `RestaurantAccount`

> Target layer: **Application** (Clean Architecture). Backed by Domain aggregate `RestaurantAccount` and the `AccountTransaction` read/audit entity. Aligns with overall YummyZoom architecture (DDD + CQRS).

---

## 0) Overview & Scope Alignment

`RestaurantAccount` is the **financial balance** aggregate for a restaurant. It owns the mutable balance (`CurrentBalance`) and payout configuration (`PayoutMethodDetails`), and emits domain events for audit trails and integrations. **History** (credits/debits) lives outside the aggregate as immutable `AccountTransaction` records produced by event handlers.

Primary goals in the Application layer:

* Expose **commands** that safely mutate account state through aggregate methods.
* Expose **queries** optimized for dashboards (Dapper/SQL) and back-office needs.
* Handle **domain events** to create `AccountTransaction` audit rows and integrate with payouts infrastructure.
* Enforce **authorization** and **cross-aggregate checks** (e.g., order existence).
* Provide **idempotency**, **observability**, and **hard invariants** mapping to domain errors.

---

## 1) Core Use Cases & Actors

| Actor                       | Use Case / Goal                            | Description                                                                           |
| --------------------------- | ------------------------------------------ | ------------------------------------------------------------------------------------- |
| **System (Order pipeline)** | Record restaurant revenue for a paid order | After `OrderPaid`, credit net revenue to the restaurant account.                      |
| **System (Fees engine)**    | Record platform fee                        | Deduct platform fee (negative amount) on eligible orders.                             |
| **System (Refunds)**        | Record refund deduction                    | Deduct refunds from balance (negative amount) when a refund is issued.                |
| **Restaurant Owner**        | Configure payout method                    | Add/update payout destination (tokenized bank info).                                  |
| **Restaurant Owner**        | Settle a payout                            | Transfer a positive amount out of the current balance, subject to sufficiency checks. |
| **Admin**                   | Make a manual adjustment                   | Apply a positive/negative adjustment with a reason (e.g., dispute resolution).        |
| **Admin**                   | Create/Delete (soft) restaurant account    | Provision or mark as deleted for governance.                                          |
| **Owner/Staff/Admin**       | View balance & history                     | See balance, transactions, and summaries for dashboards & reconciliation.             |

---

## 2) Commands (Write Operations)

> **Conventions**: MediatR commands returning `Result<T>` or `Result`. Validation via FluentValidation; transactional boundary via `IUnitOfWork.ExecuteInTransactionAsync`.

### 2.1 Command Catalog

| Command                            | Actor/Trigger                | Key Parameters                                       | Response DTO                                 | Authorization                                     |
| ---------------------------------- | ---------------------------- | ---------------------------------------------------- | -------------------------------------------- | ------------------------------------------------- |
| **CreateRestaurantAccountCommand** | Admin (or provisioning flow) | `RestaurantId`                                       | `CreateRestaurantAccountResponse(AccountId)` | Admin or System provisioner                       |
| **RecordRevenueCommand**           | System (OrderDelivered)           | `RestaurantId`, `OrderId`, `Amount`                  | `Result.Success()`                           | System; idempotent per (AccountId, OrderId, Type) |
| **RecordPlatformFeeCommand**       | System (Fees)                | `RestaurantId`, `OrderId`, `FeeAmount` (negative)    | `Result.Success()`                           | System; idempotent                                |
| **RecordRefundDeductionCommand**   | System (Refunds)             | `RestaurantId`, `OrderId`, `RefundAmount` (negative) | `Result.Success()`                           | System; idempotent                                |
| **SettlePayoutCommand**            | Restaurant Owner             | `RestaurantId`, `Amount` (>0)                        | `SettlePayoutResponse(PayoutId, NewBalance)` | Owner; KYC verified & payout method required      |
| **MakeManualAdjustmentCommand**    | Admin                        | `RestaurantId`, `Amount` (+/-), `Reason`, `AdminId`  | `Result.Success()`                           | Admin only                                        |
| **UpdatePayoutMethodCommand**      | Restaurant Owner             | `RestaurantId`, `PayoutMethodDetailsDto`             | `Result.Success()`                           | Owner of Restaurant                               |
| **DeleteRestaurantAccountCommand** | Admin                        | `RestaurantId`                                       | `Result.Success()`                           | Admin only                                        |

### 2.2 Command → Aggregate Method Mapping & Error Surface

| Command                 | Aggregate Method                                 | Domain Invariants enforced                                                         | Typical Failures surfaced to API                            |
| ----------------------- | ------------------------------------------------ | ---------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| CreateRestaurantAccount | `RestaurantAccount.Create(restaurantId)`         | New account starts with 0 balance; single account per restaurant (checked in repo) | 409 Conflict if already exists                              |
| RecordRevenue           | `RecordRevenue(Money > 0, orderId)`              | Amount must be positive                                                            | 400 `OrderRevenueMustBePositive`                            |
| RecordPlatformFee       | `RecordPlatformFee(Money < 0, orderId)`          | Amount must be negative                                                            | 400 `PlatformFeeMustBeNegative`                             |
| RecordRefundDeduction   | `RecordRefundDeduction(Money < 0, orderId)`      | Amount must be negative                                                            | 400 `RefundDeductionMustBeNegative`                         |
| SettlePayout            | `SettlePayout(Money > 0)`                        | Amount > 0 and ≤ `CurrentBalance`                                                  | 400 `PayoutAmountMustBePositive`; 422 `InsufficientBalance` |
| MakeManualAdjustment    | `MakeManualAdjustment(Money ±, reason, adminId)` | Non-empty reason                                                                   | 400 `ManualAdjustmentReasonRequired`                        |
| UpdatePayoutMethod      | `UpdatePayoutMethod(PayoutMethodDetails)`        | Valid value object                                                                 | 400 `InvalidPayoutMethod`                                   |
| DeleteRestaurantAccount | `MarkAsDeleted()`                                | Emits deletion event                                                               | 200 OK                                                      |

**Cross-aggregate checks (Application layer):**

* For `RecordRevenue`/`RecordPlatformFee`/`RecordRefundDeduction`, verify `Order` exists and belongs to the same `RestaurantId` (read model query). Reject otherwise.
* For `SettlePayout`, require `PayoutMethodDetails` present; optionally require KYC/verification flags from `Restaurant`/compliance service.

---

## 3) Queries (Read Operations)

> **Implementation**: Dapper SQL; tailored DTOs; indices on `(RestaurantId, Timestamp)` for transaction tables.

### 3.1 Query Catalog

| Query                                | Actor             | Key Parameters                                          | Response DTO                                                                                 | SQL Highlights / Tables                                                                                        |
| ------------------------------------ | ----------------- | ------------------------------------------------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| **GetRestaurantAccountSummaryQuery** | Owner/Staff/Admin | `RestaurantId`                                          | `RestaurantAccountSummaryDto` (Balance, LastPayoutAt, LastTransactionAt, PayoutMethodMasked) | `SELECT CurrentBalance, PayoutMethod, ... FROM RestaurantAccounts WHERE RestaurantId=@id`                      |
| **GetRestaurantAccountBalanceQuery** | Owner/Staff/Admin | `RestaurantId`                                          | `MoneyDto`                                                                                   | Same as above                                                                                                  |
| **ListAccountTransactionsQuery**     | Owner/Staff/Admin | `RestaurantId`, `From`, `To`, `Type?`, `Page`           | `PaginatedList<AccountTransactionDto>`                                                       | `SELECT ... FROM AccountTransactions WHERE RestaurantId=@id AND Timestamp BETWEEN ... ORDER BY Timestamp DESC` |
| **GetPayoutHistoryQuery**            | Owner/Staff/Admin | `RestaurantId`                                          | `PaginatedList<PayoutDto>`                                                                   | Filter `AccountTransactions.Type = PayoutSettlement`                                                           |
| **GetRevenueVsFeesSummaryQuery**     | Owner/Staff/Admin | `RestaurantId`, `From`, `To`, `Bucket` (day/week/month) | `Series<SummaryPointDto>`                                                                    | `GROUP BY date_trunc(...)` splitting by `Type`                                                                 |
| **GetPayoutEligibilityQuery**        | Owner             | `RestaurantId`                                          | `PayoutEligibilityDto`                                                                       | Compute `CurrentBalance`, min payout threshold, holds/reserves if any                                          |
| **SearchRestaurantAccountsQuery**    | Admin             | `Text`, `IsActive?`, `MinBalance?`                      | `PaginatedList<RestaurantAccountAdminRowDto>`                                                | Back-office filters                                                                                            |

### 3.2 DTO Sketches

* `RestaurantAccountSummaryDto { MoneyDto CurrentBalance; bool HasPayoutMethod; string? PayoutMethodMasked; DateTimeOffset? LastPayoutAt; DateTimeOffset? LastTransactionAt }`
* `AccountTransactionDto { Guid Id; Guid RestaurantId; string Type; MoneyDto Amount; DateTimeOffset Timestamp; Guid? RelatedOrderId; string? Notes }`
* `PayoutDto { Guid Id; MoneyDto Amount; DateTimeOffset SettledAt; MoneyDto BalanceAfter }`
* `PayoutEligibilityDto { MoneyDto EligibleAmount; bool HasPayoutMethod; string? MissingReason }`

---

## 4) Domain Events & Application Handlers

> Domain events emitted by the aggregate drive audit trail creation and integrations. Handlers are **asynchronous** (fire-and-forget with outbox) and **idempotent**.

| Domain Event               | Emitted When     | Application Handlers                                                                                    | Responsibilities                                                                                                                       |
| -------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `RestaurantAccountCreated` | Account created  | `EnsureAccountProjection` (sync in UoW if you seed local projections), `PublishAccountCreated` (outbox) | Seed minimal local views; publish integration event.                                                                                   |
| `RevenueRecorded`          | Revenue credited | `CreateAccountTransaction(OrderRevenue)` (**sync in UoW**), `PublishRevenueRecorded` (outbox)           | Insert **positive** immutable row with `RelatedOrderId`; update denorm summaries if they live locally; publish for external consumers. |
| `PlatformFeeRecorded`      | Fee debited      | `CreateAccountTransaction(PlatformFee)` (**sync in UoW**), `PublishPlatformFeeRecorded` (outbox)        | Insert **negative** row with `RelatedOrderId`.                                                                                         |
| `RefundDeducted`           | Refund debited   | `CreateAccountTransaction(RefundDeduction)` (**sync in UoW**), `PublishRefundDeducted` (outbox)         | Insert **negative** row with `RelatedOrderId`.                                                                                         |
| `PayoutSettled`            | Payout succeeds  | `CreateAccountTransaction(PayoutSettlement)` (**sync in UoW**), `PublishPayoutSettled` (outbox)         | Insert payout row (typically **negative**); publish to payments/BI.                                                                    |
| `ManualAdjustmentMade`     | Admin adjustment | `CreateAccountTransaction(ManualAdjustment)` (**sync in UoW**), `PublishManualAdjustment` (outbox)      | Insert row (any sign) with notes/actor; publish.                                                                                       |

**Integration events consumed by Application layer:**

* `OrderPaid` (from Order context) → orchestrates `RecordRevenueCommand` and `RecordPlatformFeeCommand`.
* `OrderRefunded` (from Order/Support contexts) → orchestrates `RecordRefundDeductionCommand`.

---

## 5) Orchestration for Complex Flows

### 5.1 `SettlePayoutCommandHandler`

1. **Validate**: amount > 0; currency matches platform.
2. **Authorize**: caller is Restaurant Owner for `RestaurantId`; restaurant is verified.
3. **Load**: `RestaurantAccount` by `RestaurantId` via repo.
4. **Pre-checks** (read models/services): payout method exists; holds/reserves cleared; min threshold satisfied.
5. **Invoke Aggregate**: `account.SettlePayout(amount)` → may return `InsufficientBalance`.
6. **Persist**: repo update; commit transaction.
7. **Side effects (async)**: outbox publishes `PayoutSettled` → `CreateAccountTransaction(PayoutSettlement)`; call payments provider with idempotency key (AccountId + timestamp + amount); send owner notification.
8. **Return**: `SettlePayoutResponse` (payout id, new balance).

### 5.2 `RecordRevenue` via `OrderDelivered`

1. Receive `OrderDelivered(orderId, restaurantId, amounts...)` and validate ownership, delivery status.
2. **Idempotency check**: ensure no `AccountTransaction` of type `OrderRevenue` exists for `(RestaurantAccountId, RelatedOrderId, Type=OrderRevenue)`.
3. **Load** `RestaurantAccount`; create if auto-provisioning enabled.
4. **Call** `restaurantAccount.RecordRevenue(amount, orderId)` (aggregate raises `RevenueRecorded` internally).
5. **Create** `AccountTransaction` (Type=OrderRevenue, amount>0, RelatedOrderId required).
6. **Persist** both the aggregate and the `AccountTransaction` in the same DB transaction.
7. Add an **outbox** message for `RevenueRecorded` (for dashboards/analytics/ETL).
8. **Post-processing**: refresh revenue summaries; optionally record `PlatformFee` in same flow based on fee schedule.
9. Commit transaction.

---

## 6) Read Models & Projections (CQRS)

> Immutable audit: `AccountTransactions` (Type, Amount, Timestamp, RelatedOrderId, Notes). Denormalized summaries for dashboards.

Recommended projections:

* **`AccountTransactions`**: append-only; indexed by `(RestaurantId, Timestamp)` and `(RelatedOrderId)`.
* **`RestaurantAccountDashboard`**: `{RestaurantId, CurrentBalance, LastPayoutAt, Last30dRevenue, Last30dFees, Last30dRefunds}` maintained by event handlers.
* **`PayoutHistory`**: separate view (or filter on transactions) for quick access to settlements.

---

## 7) API Surface (Web Layer Endpoints)

> Minimal APIs/Controllers call Application commands/queries. All endpoints return `Result<>` shapes from `SharedKernel`.

### 7.1 Owner/Admin APIs

* `POST /restaurants/{restaurantId}/account/payouts`: `SettlePayoutCommand` → `201 Created` with payout resource.
* `PUT /restaurants/{restaurantId}/account/payout-method`: `UpdatePayoutMethodCommand` → `204 No Content`.
* `GET /restaurants/{restaurantId}/account/summary`: `GetRestaurantAccountSummaryQuery`.
* `GET /restaurants/{restaurantId}/account/transactions`: `ListAccountTransactionsQuery` (paging, type filter, date range).

### 7.2 System/Admin APIs

* `POST /internal/accounts/{restaurantId}/revenue`: `RecordRevenueCommand` (idempotent).
* `POST /internal/accounts/{restaurantId}/fees`: `RecordPlatformFeeCommand` (idempotent).
* `POST /internal/accounts/{restaurantId}/refunds`: `RecordRefundDeductionCommand` (idempotent).
* `POST /admin/restaurants/{restaurantId}/account/manual-adjustment`: `MakeManualAdjustmentCommand`.
* `POST /admin/restaurants/{restaurantId}/account`: `CreateRestaurantAccountCommand`.
* `DELETE /admin/restaurants/{restaurantId}/account`: `DeleteRestaurantAccountCommand` (soft delete).

---

## 8) Suggested Refinements (Domain & Code)

1. **Value Object structure for `PayoutMethodDetails`**: evolve from `string Details` to a richer, typed shape (e.g., `{ Provider, Token, MaskedDisplay, Last4, Country, Currency }`) while preserving tokenization and immutability.
2. **Optimistic concurrency**: add a `RowVersion`/`ConcurrencyStamp` to `RestaurantAccount` to guard concurrent postings from different flows.
3. **Currency discipline**: ensure `Money` enforces the platform currency or add multi-currency support at the boundary, with conversion rules.
4. **Deletion semantics**: if `MarkAsDeleted` is used, consider prohibiting any further postings at the repository level (guard in app service) and emit warnings if downstream events try to mutate.
5. **Minimum payout thresholds and rolling reserves**: represent as configuration per restaurant/region; pre-check in `SettlePayout` handler.
6. **Outbox + Inbox**: adopt inbox processing for external events (e.g., `OrderPaid`) to guarantee idempotent consumption.
7. **Unique constraints**: database constraint to ensure **one account per restaurant**.
8. **Balance-at-time** snapshot: include `BalanceAfter` on all transaction audit rows for easier reconciliation.
9. **Back-pressure**: if payments provider fails, keep `PayoutSettled` as *initiated* event and follow with `PayoutCompleted`/`PayoutFailed` events for accurate state (optional extension).

---

## 9) Open Questions

* Should platform fees be recorded **net** (as part of revenue calculation) or **gross + fee** (separate transaction)? Current design supports separate transactions; confirm accounting policy.
* Do we need **holds/reserves** logic (temporary balance locks) for disputes or high-risk orders? If yes, introduce a `Hold` concept in read models and Application pre-checks.
* Required **KYC/AML** checks before enabling payouts—what are the regional rules?

---

**End of Feature Discovery — `RestaurantAccount`.**
