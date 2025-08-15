## Feature Discovery & Application Layer Design — `AccountTransaction`

> Target layer: **Application**. Bounded to the **Payouts & Monetization** context. `AccountTransaction` is an **immutable audit entity** created by handlers reacting to `RestaurantAccount` domain events (e.g., `RevenueRecorded`, `PayoutSettled`). It is **not** mutated directly by user commands.

---

## 0) Overview & Scope Alignment

`AccountTransaction` records the authoritative audit trail of money movements affecting a `RestaurantAccount` (credits/debits per `TransactionType`). It owns: `AccountTransactionId`, `RestaurantAccountId`, `TransactionType`, `Money Amount` (with sign semantics), `Timestamp`, optional `RelatedOrderId`, and `Notes`. Creation occurs **only** via Application event handlers consuming `RestaurantAccount` domain events (outbox → inbox). Historical reporting and exports are served via queries and read-optimized views.

Primary goals in the Application layer:

* **Project** domain events into immutable `AccountTransaction` rows (idempotently).
* **Expose queries** for ledger views, reconciliation, and statements.
* **Enforce cross-aggregate checks** (e.g., order/restaurant ownership consistency).
* **Support operations**: export, pagination, drill-down, and payout statements.

---

## 1) Core Use Cases & Actors

| Actor                        | Use Case / Goal        | Description                                                                                                                   |
| ---------------------------- | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| **System (Event Processor)** | Append audit row       | Consume `RestaurantAccount` domain events and materialize `AccountTransaction` with strict sign rules and referential checks. |
| **Restaurant Owner/Staff**   | View ledger            | List/filter transactions, inspect details, export CSV for reconciliation.                                                     |
| **Platform Admin / Finance** | Oversight & statements | Produce statements for a date range, verify fees/refunds, investigate disputes.                                               |
| **Support Agent**            | Case investigation     | Drill into a transaction linked to an `Order` during a refund/dispute.                                                        |

---

## 2) Commands (Write Operations)

> Convention: MediatR commands return `Result<T>`/`Result`. Transactions themselves are **not directly created by public commands**. They are materialized by handlers (see §4). The only public write in this feature is **operational** (no domain mutation): exports.

### 2.1 Command Catalog

| Command                                | Actor/Trigger | Key Parameters                                                          | Response DTO                      | Authorization                |
| -------------------------------------- | ------------- | ----------------------------------------------------------------------- | --------------------------------- | ---------------------------- |
| **`ExportAccountTransactionsCommand`** | Owner/Admin   | `RestaurantAccountId`, `DateRange`, `Types?`, `OrderId?`, `Format`(CSV) | `ExportFileDto { Uri, FileName }` | Owner of restaurant or Admin |

### 2.2 Command → Aggregate Method Mapping & Error Surface

*(N/A for `AccountTransaction` creation; see §4 for event → projection mapping.)*

**Cross-aggregate checks (Application layer):**

* Verify `RestaurantAccount` exists and belongs to the authenticated restaurant.
* If `RelatedOrderId` is present, ensure the order belongs to the same `Restaurant`.

---

## 3) Queries (Read Operations)

> Implementation: Dapper/SQL read models optimized for filters and sorting; indices on `(RestaurantAccountId, Timestamp DESC)` and on `Type`, `RelatedOrderId`.

### 3.1 Query Catalog

| Query                                   | Actor               | Key Parameters                                                     | Response DTO                                                                | SQL Highlights / Tables                                                                                                             |
| --------------------------------------- | ------------------- | ------------------------------------------------------------------ | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| **`ListAccountTransactionsQuery`**      | Owner/Admin         | `RestaurantAccountId`, `DateRange`, `Types[]?`, `OrderId?`, `Page` | `PaginatedList<AccountTransactionRowDto>`                                   | `WHERE RestaurantAccountId = @... AND Timestamp BETWEEN ... AND (Type IN ...)? AND (RelatedOrderId = ...)? ORDER BY Timestamp DESC` |
| **`GetAccountTransactionDetailsQuery`** | Owner/Admin/Support | `AccountTransactionId`                                             | `AccountTransactionDetailsDto`                                              | `SELECT ... WHERE Id = @Id` (+ join to lightweight order snapshot if present)                                                       |
| **`GetAccountTransactionSummaryQuery`** | Owner/Admin         | `RestaurantAccountId`, `DateRange`                                 | `AccountTransactionSummaryDto { TotalCredits, TotalDebits, Net, ByType[] }` | `GROUP BY Type`                                                                                                                     |
| **`GetPayoutStatementQuery`**           | Owner/Admin         | `RestaurantAccountId`, `StatementPeriod`                           | `PayoutStatementDto`                                                        | Aggregates `OrderRevenue`, `PlatformFee`, `RefundDeduction`, `PayoutSettlement` for period                                          |

### 3.2 DTO Sketches

* `AccountTransactionRowDto { Id, Timestamp, Type, Amount, Currency, RelatedOrderId?, Notes }`
* `AccountTransactionDetailsDto { ...RowDto, RestaurantAccountId, CreatedBy?, OrderNumber?, OrderTotal? }`
* `AccountTransactionSummaryDto { PeriodStart, PeriodEnd, TotalCredits, TotalDebits, Net, ByType: [{ Type, Total, Count }] }`
* `PayoutStatementDto { Period, OpeningBalance?, Credits, Debits, NetChange, Payouts:[{ Id, Amount, Timestamp }], Notes }`

---

## 4) Domain Events & Application Handlers

> Handlers are **asynchronous** (outbox) and **idempotent**. Each handler validates sign rules and referential integrity, then calls the domain factory to create the `AccountTransaction` row.

| Domain Event                                               | Emitted When                        | Application Handlers                     | Responsibilities                                                                           |
| ---------------------------------------------------------- | ----------------------------------- | ---------------------------------------- | ------------------------------------------------------------------------------------------ |
| `RevenueRecorded(RestaurantAccountId, OrderId, Money)`     | Order capture recognized as revenue | `OnRevenueRecordedCreateTransaction`     | Ensure `Order` belongs to restaurant; create **`OrderRevenue`** (amount **> 0**); persist. |
| `PlatformFeeRecorded(RestaurantAccountId, OrderId, Money)` | Platform fee applied                | `OnPlatformFeeRecordedCreateTransaction` | Create **`PlatformFee`** (amount **< 0**); persist.                                        |
| `RefundDeducted(RestaurantAccountId, OrderId, Money)`      | Refund debited from restaurant      | `OnRefundDeductedCreateTransaction`      | Create **`RefundDeduction`** (amount **< 0**); persist.                                    |
| `PayoutSettled(RestaurantAccountId, Money)`                | Payout to restaurant                | `OnPayoutSettledCreateTransaction`       | Create **`PayoutSettlement`** (amount typically **< 0**); persist.                         |
| `ManualAdjustmentMade(RestaurantAccountId, Money, Notes)`  | Admin manual balance change         | `OnManualAdjustmentCreateTransaction`    | Create **`ManualAdjustment`** (any sign); persist with notes.                              |

**Idempotency:** use event `MessageId` (or composite of type+account+order+amount+timestamp) as an inbox key; ensure **unique index** on `(SourceEventId)` in the `AccountTransactions` table or maintain a processed-events table.

---

## 5) Orchestration for Complex Flows

### 5.1 Payout Settlement (Statement Period Close)

1. Billing job computes period totals for a `RestaurantAccount`.
2. Application issues `SettlePayout(amount)` on the `RestaurantAccount` aggregate → emits `PayoutSettled`.
3. Outbox publishes; inbox handler `OnPayoutSettledCreateTransaction` writes `AccountTransaction` and updates statement view.
4. `GetPayoutStatementQuery` returns the statement, including payout rows and net change.

### 5.2 Refund Flow (Post-Delivery Dispute)

1. Support approves refund on an order.
2. Application command triggers aggregate method to **deduct refund** from `RestaurantAccount` → emits `RefundDeducted`.
3. Handler writes `RefundDeduction` transaction (negative) linked to `OrderId`.
4. Ledger and statement reflect the debit; owner can drill down via `GetAccountTransactionDetailsQuery`.

---

## 6) Read Models & Projections (CQRS)

* **`AccountTransaction` table (write model)**: immutable, append-only; clustered by `Id`, indexed by `(RestaurantAccountId, Timestamp DESC)`, `(Type)`, `(RelatedOrderId)`.
* **`AccountTransactionDailySummaryView`**: `{ RestaurantAccountId, Date, Credits, Debits, Net }` maintained by handlers; backs charts and quick summaries.
* **`PayoutStatementView`**: denormalized view/document for a statement period, listing all included transactions and computed totals.

---

## 7) API Surface (Web Layer Endpoints)

* `GET /accounts/{restaurantAccountId}/transactions` → `ListAccountTransactionsQuery`
* `GET /accounts/{restaurantAccountId}/transactions/summary` → `GetAccountTransactionSummaryQuery`
* `GET /accounts/{restaurantAccountId}/transactions/{transactionId}` → `GetAccountTransactionDetailsQuery`
* `POST /accounts/{restaurantAccountId}/transactions:export` → `ExportAccountTransactionsCommand` (CSV)
* *(Event-driven creations are internal; no public POST for transactions)*

All endpoints return `Result<>` envelopes and enforce role/ownership checks.

---

## 8) Suggested Refinements (Domain & Code)

1. **Require `RelatedOrderId`** for types that logically need it (`OrderRevenue`, `PlatformFee`, `RefundDeduction`) — validation at factory and handler levels.
2. Prefer **`DateTimeOffset`** for `Timestamp` (domain currently uses `DateTime`); keep `Created` audit already `DateTimeOffset`.
3. Add a **unique constraint** to prevent duplicate rows from the same event: e.g., `UNIQUE(SourceEventId)` or a natural key across `(Type, RestaurantAccountId, RelatedOrderId?, Amount, Timestamp bucket)`.
4. Ensure **Money** includes currency and arithmetic guards; normalize currency at account level.
5. Add **partial indexes** (or filtered indexes) per `Type` for fast fee/refund lookups.
6. Consider a **soft-deletion guard** via a separate `Correction`/`Reversal` mechanism (add-on feature) rather than editing rows; expose `ReversalOfTransactionId` if needed.
7. Add **RowVersion** to the table for defensive reads (even if rows are immutable) to help with replication diagnostics.
8. Outbox/inbox pipelines should carry **idempotency keys** and **occurredAt** timestamps; log structured event metadata for reconciliation.

---

## 9) Open Questions

* Do we need a distinct `Reversal` transaction type (with linkage) for audit-friendly corrections?
* Should `PayoutSettlement` always be negative? (Some systems model it as a credit to a separate external ledger.)
* Should the ledger expose **opening/closing balances per period** directly, or compute on the fly in the statement view?
* Any regulatory/reporting formats (e.g., VAT/GST breakdown) to include in statements?

---

**End — AccountTransaction Application Feature Discovery.**
