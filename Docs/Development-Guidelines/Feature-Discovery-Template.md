## Feature Discovery & Application Layer Design — `[AggregateName]`

> Target layer: **Application** (Clean Architecture). Backed by Domain aggregate `[AggregateName]` and any relevant read/audit entities. Aligns with overall YummyZoom architecture (DDD + CQRS).

---

## 0) Overview & Scope Alignment

`[AggregateName]` is the `[short, high-level description of purpose and boundaries]`. It owns `[core mutable state/value objects]` and emits domain events for `[audit/integrations/projections]`. Historical records, if needed, live outside the aggregate as immutable `[ReadModelName]` rows produced by event handlers.

Primary goals in the Application layer:

* Expose **commands** that safely mutate aggregate state through aggregate methods.
* Expose **queries** optimized for dashboards (Dapper/SQL) and back-office needs.
* Handle **domain events** to create audit rows and integrate with external systems.
* Enforce **authorization** and **cross-aggregate checks**.
* Provide **idempotency**, **observability**, and **hard invariants** mapping to domain errors.

---

## 1) Core Use Cases & Actors

| Actor                       | Use Case / Goal                     | Description |
| --------------------------- | ----------------------------------- | ----------- |
| `[Role or System]`          | `[Primary action for this actor]`   | `[One-line description of what happens and why]` |
| `[Role or System]`          | `[Another action]`                  | `[Description]` |
| `[Admin/Owner/Staff]`       | `[Administrative/owner action]`     | `[Description]` |
| `[Viewer/External System]`  | `[Read/reporting action]`           | `[Description]` |

---

## 2) Commands (Write Operations)

> Conventions: MediatR commands returning `Result<T>` or `Result`. Validation via FluentValidation; transactional boundary via `IUnitOfWork.ExecuteInTransactionAsync`.

### 2.1 Command Catalog

| Command                            | Actor/Trigger           | Key Parameters                                   | Response DTO                                 | Authorization |
| ---------------------------------- | ----------------------- | ------------------------------------------------ | -------------------------------------------- | ------------- |
| **`[Create][AggregateName]Command`** | `[Admin/User/System]`   | `[AggregateId?]`, `[RequiredDtos]`               | `[CreateXResponse(Id)]`                      | `[Policy/Role]` |
| **`[Action]Command`**              | `[Role/System]`         | `[AggregateId]`, `[OtherIds]`, `[ValueObjects]`  | `Result.Success()`                           | `[Policy/Role]` |
| **`[Action]Command`**              | `[Role/System]`         | `[Parameters]`                                   | `[ResponseDto]`                               | `[Policy/Role]` |

### 2.2 Command → Aggregate Method Mapping & Error Surface

| Command                  | Aggregate Method                             | Domain Invariants enforced                                 | Typical Failures surfaced to API |
| ------------------------ | -------------------------------------------- | ---------------------------------------------------------- | ------------------------------- |
| `[Create...]`            | `[AggregateName].Create(...)`                | `[e.g., unique per owner, initial state]`                  | `[409 Conflict / 400 ...]` |
| `[Action...]`            | `[AggregateMethod(ValueObject, ids...)]`     | `[e.g., value sign/constraints, state preconditions]`      | `[400/422 specific domain errors]` |
| `[Delete/Archive...]`    | `[MarkAsDeleted/Archive]`                    | `[emits deletion event, no further mutations allowed?]`    | `200 OK` |

**Cross-aggregate checks (Application layer):**

* `[Example: verify related aggregate exists and belongs to same owner/context]`
* `[Example: ensure prerequisites such as configuration/KYC/external state]`

---

## 3) Queries (Read Operations)

> Implementation: Dapper SQL; tailored DTOs; indices appropriate for query predicates.

### 3.1 Query Catalog

| Query                               | Actor              | Key Parameters                              | Response DTO                                 | SQL Highlights / Tables |
| ----------------------------------- | ------------------ | ------------------------------------------- | -------------------------------------------- | ----------------------- |
| **`Get[Aggregate]SummaryQuery`**    | `[Role(s)]`        | `[AggregateId or Filters]`                   | `[SummaryDto]`                                | `SELECT ... FROM [Table] WHERE ...` |
| **`Get[Aggregate]DetailsQuery`**    | `[Role(s)]`        | `[AggregateId]`                              | `[DetailsDto]`                                | `JOIN ...` |
| **`List[Aggregate]ItemsQuery`**     | `[Role(s)]`        | `[Filter, Page]`                             | `PaginatedList<[RowDto]>`                     | `WHERE ... ORDER BY ...` |
| **`Search[Aggregate]Query`**        | `[Role(s)]`        | `[Text, Filters]`                            | `PaginatedList<[SearchResultDto]>`            | `[FTS, indexes, etc.]` |

### 3.2 DTO Sketches

* `[SummaryDto { ... }]`
* `[RowDto { ... }]`
* `[DetailsDto { ... }]`

---

## 4) Domain Events & Application Handlers

> Domain events emitted by the aggregate drive audit/projections/integrations. Handlers are **asynchronous** (outbox) and **idempotent**.

| Domain Event                | Emitted When            | Application Handlers                       | Responsibilities |
| --------------------------- | ----------------------- | ------------------------------------------ | ---------------- |
| `[AggregateCreated]`        | `[on create]`           | `[EnsureProjection]`                        | `[Seed projections / notify]` |
| `[MeaningfulEvent]`         | `[on state change]`     | `[CreateAuditRow(...)]`                     | `[Append immutable audit row]` |
| `[AnotherEvent]`            | `[on action]`           | `[TriggerExternalIntegration]`              | `[Call provider with idempotency key]` |
| `[Deleted/Archived]`        | `[on delete/archive]`   | `[FlagProjection/HideFromUI]`               | `[Governance, prevent postings]` |

**Integration events consumed by Application layer:**

* `[InboundEventName]` → orchestrates `[CommandName]`.
* `[InboundEventName]` → orchestrates `[CommandName]`.

---

## 5) Orchestration for Complex Flows

### 5.1 `[ImportantCommand]CommandHandler`

1. **Validate**: `[rules]`.
2. **Authorize**: `[policies/ownership checks]`.
3. **Load**: `[AggregateName]` by id via repo.
4. **Pre-checks**: `[required configuration/external state/thresholds]`.
5. **Invoke Aggregate**: ``aggregate.[Method](...)`` → may return domain errors.
6. **Persist**: repo update; commit transaction.
7. **Side effects (async)**: outbox publishes events; trigger external processes; notify users.
8. **Return**: `[Response DTO / Result]`.

### 5.2 `[InboundEvent]` → `[CommandName]`

1. Receive `[InboundEvent(args...)]`.
2. **Idempotency check**: `[ensure not processed / no duplicate audit row exists]`.
3. **Load** `[AggregateName]`; `[create if auto-provisioning enabled?]`.
4. **Call** `[Command/AggregateMethod]`; persist.
5. **Emit** `[DomainEvent]` → handler writes audit/projection and triggers any integrations.

---

## 6) Read Models & Projections (CQRS)

> Immutable audit: `[AuditTable] (Type, Amount/Value, Timestamp, RelatedId, Notes)`. Denormalized summaries for dashboards.

Recommended projections:

* **`[AuditTable]`**: append-only; indexed by `[keys]`.
* **`[DashboardView]`**: `{[Key fields and computed summaries]}` maintained by event handlers.
* **`[HistoryView]`**: separate view (or filter) for quick access to specific subsets.

---

## 7) API Surface (Web Layer Endpoints)

> Minimal APIs/Controllers call Application commands/queries. All endpoints return `Result<>` shapes from `SharedKernel`.

### 7.1 User/Admin APIs

* `POST /[resource]` → `[CreateCommand]` → `201 Created` with resource id.
* `PUT /[resource]/{id}` → `[UpdateCommand]` → `204 No Content`.
* `GET /[resource]/{id}/summary` → `[GetSummaryQuery]`.
* `GET /[resource]/{id}/items` → `[ListItemsQuery]` (paging, filters).

### 7.2 System/Admin APIs

* `POST /internal/[resource]/[action]` → `[SystemCommand]` (idempotent).
* `POST /admin/[resource]/[action]` → `[AdminCommand]`.
* `DELETE /admin/[resource]/{id}` → `[DeleteCommand]` (soft delete).

---

## 8) Suggested Refinements (Domain & Code)

1. `[Value Object structure improvements]`.
2. `[Optimistic concurrency / RowVersion]`.
3. `[Currency/units discipline or normalization rules]`.
4. `[Deletion semantics and guards]`.
5. `[Thresholds/reserves/holds as configuration]`.
6. `[Outbox + Inbox for idempotency]`.
7. `[Unique constraints at DB level]`.
8. `[Snapshot fields to ease reconciliation]`.
9. `[Back-pressure and state transitions for external providers]`.

---

## 9) Open Questions

* `[Accounting policy / modeling choice?]`
* `[Do we need additional holds/reserves/limits?]`
* `[Regulatory/KYC requirements?]`

---

**End of Feature Discovery — `[AggregateName]`.**
