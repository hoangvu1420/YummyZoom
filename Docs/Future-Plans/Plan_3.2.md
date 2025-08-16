Plan 3.2

Implementing Orders Queries – outlining detailed steps (no code) to add the five queries aligned with existing CQRS/Dapper patterns.

## 🔄 Pattern Alignment Review (Current Codebase Findings)
Observed existing query & infrastructure patterns in `src/Application`:
1. Two query styles coexist:
  - Legacy/EF style (e.g., `GetTodoItemsWithPaginationQuery`) using EF IQueryable + `PaginatedList<T>.CreateAsync`.
  - New CQRS read model style (e.g., `GetUserRoleAssignmentsQuery`, `GetRestaurantRoleAssignmentsWithUserInfoQuery`) using `IDbConnectionFactory` + Dapper with raw SQL and record-based response wrappers.
2. Dapper queries currently do NOT provide a standardized pagination helper (no `PaginatedList<T>` creation from raw SQL) – pagination example only exists in EF variant.
3. Authorization:
  - Commands use `[Authorize]` attributes + contextual interfaces (`IRestaurantCommand`, `IUserCommand`) implementing `IContextualCommand` enabling resource-aware policy evaluation in `AuthorizationBehaviour`.
  - Queries so far do NOT implement contextual interfaces; role assignment queries are unprotected (administrative endpoints likely rely on controller-level auth) – we will introduce contextual query interfaces to align with the command pattern for restaurant-scoped + order-scoped authorization.
4. Response Wrapping Pattern:
  - Dapper queries: `Result<GetXResponse>` where `GetXResponse` contains a collection or DTO root.
  - EF paginated query: returns `Result<PaginatedList<Dto>>` directly without an extra wrapper.
  - We'll stay consistent: list-style Dapper queries will return `Result<PaginatedList<OrderSummaryDto>>` directly (mirroring EF pagination style) to avoid an unnecessary additional response layer, while single-entity detail queries will use a response wrapper record for clarity (`GetOrderByIdResponse`).
5. DTO Naming: Uses `*Dto` or `*ResultDto` (for commands). We'll adopt `OrderDetailsDto`, `OrderSummaryDto`, `OrderStatusDto` and reuse `PaginatedList<OrderSummaryDto>`.
6. Errors Pattern: Command handlers define static error classes (e.g., `AcceptOrderErrors`). Queries that can fail with NotFound currently inline logic and return `Result.Success` with empty list when applicable. For detail queries we will define `GetOrderByIdErrors.NotFound` to keep parity with command error style.
7. Cancellation & Dapper Usage: Existing Dapper handlers correctly use `CommandDefinition(sql, new { ... }, cancellationToken: token)` – replicate this.

Implications / Enhancements Needed:
| Concern | Gap | Enhancement |
|---------|-----|-------------|
| Dapper Pagination | No helper to create `PaginatedList<T>` from SQL | Introduce reusable helper (extension or static method) performing count + page query (or window function) |
| Authorization for queries | No contextual query interfaces | Introduce `IRestaurantQuery` & `IOrderQuery` mirroring command interfaces for policy resource resolution |
| Order-level policies | Not yet defined (only restaurant/user) | Phase 3.2 will NOT add full order-level policies; optional future addition (document) – rely on restaurant staff policies + customer self-check logic |
| Consistency of Result wrappers | Mixed patterns | Adopt: (a) Detail -> `Result<GetOrderByIdResponse>`; (b) Lists -> `Result<PaginatedList<OrderSummaryDto>>`; (c) Lean status -> `Result<OrderStatusDto>` |
| Validation for Dapper queries | Absent in current examples | Add validators for new queries (IDs + paging) to enforce consistent input hygiene |
| Repeated status set definitions | None central yet | Add static `OrderQueryConstants` class with `ActiveStatuses` ordering & priority mapping |
| Potential N+1 on details | Need structured multi-mapping or multi-query approach | Use two queries (Order + Items+Customizations) + in-memory grouping for simplicity & maintainability |

Risk Mitigation: Add incremental tests for new pagination helper + authorization behaviour with queries implementing contextual interfaces (ensure policy is applied).

## ✅ Requirements Checklist
- Define GetOrderByIdQuery
- Define GetCustomerRecentOrdersQuery with paging
- Define GetRestaurantNewOrdersQuery (status = Placed) with paging
- Define GetRestaurantActiveOrdersQuery (status ∈ active set) with paging
- Define (optional) GetOrderStatusQuery (lean)
- Follow Application Layer Guidelines (CQRS, Dapper for reads, structure, naming)
- DTO design (no domain leakage)
- Validation (FluentValidation)
- Authorization (roles/policies)
- Pagination model reuse
- File layout under Application/Orders/Queries
- Endpoint planning (Web layer alignment)
- Testing strategy (unit + functional)
- Performance + indexing considerations
- Future extensibility (filters, sorting)
- Avoid overfetching; include only necessary joins
- Consistent Result pattern + cancellation support

## 🌳 High-Level Architectural Placement (Refined)
- Layer: Application (Queries directory under `Orders` mirroring Commands structure)
- Data Access: Dapper (`IDbConnectionFactory`) for all 5 queries – no repositories
- Mapping: Direct SQL column aliasing -> DTO construction; minimal manual mapping (grouping for detail)
- Pagination: Extend existing `PaginatedList<T>` support via new Dapper helper (does NOT modify core class signature) – keep EF path unchanged
- Authorization: Add contextual query interfaces (`IRestaurantQuery`, `IOrderQuery`) + apply `[Authorize(Policy = Policies.MustBeRestaurantStaff)]` on restaurant-scoped queries; customer-specific queries validate ownership using claims (no new order policies yet)

## 🧩 New Supporting Types / Enhancements
1. `IRestaurantQuery` (mirrors `IRestaurantCommand`) implementing `IContextualCommand`.
2. `IOrderQuery` (optional now; used for `GetOrderById` & `GetOrderStatus` to enable future policy expansion) – may initially be unused by policies but sets foundation.
3. `OrderQueryConstants` static class:
  - `public static readonly string[] ActiveStatuses = { "Placed", "Accepted", "Preparing", "ReadyForDelivery" };`
  - Priority map dictionary for ORDER BY CASE.
4. `DapperPagination` helper (static):
  - Method `QueryPageAsync<T>(IDbConnection, string baseSql, string orderByClause, string whereClause, object parameters, int pageNumber, int pageSize, CancellationToken)` returning `PaginatedList<T>` by executing (a) count query and (b) page query OR a single windowed query (decide per RDBMS; using separate queries first for clarity).
  - Overload to accept complete SQL templates if needed.
5. Error static class for detail query: `GetOrderByIdErrors`.

Open Question (deferred until schema inspection): Are monetary columns already normalized (Subtotal, Tax, etc.) or computed on the fly? Implementation step includes schema verification before final SQL authoring.

## 📁 Directory & File Structure (Refined)
`src/Application/Orders/Queries/`
```
Orders/Queries/
  Common/
    OrderDtos.cs                (OrderSummaryDto, OrderItemDto, OrderItemCustomizationDto)
    OrderDetailsDto.cs          (if size justifies separation)
    OrderQueryConstants.cs      (status sets, ordering priority)
    DapperPagination.cs         (helper)
  GetOrderById/
    GetOrderByIdQuery.cs
    GetOrderByIdQueryHandler.cs
    GetOrderByIdQueryValidator.cs
  GetCustomerRecentOrders/
    GetCustomerRecentOrdersQuery.cs
    GetCustomerRecentOrdersQueryHandler.cs
    GetCustomerRecentOrdersQueryValidator.cs
  GetRestaurantNewOrders/
    GetRestaurantNewOrdersQuery.cs
    GetRestaurantNewOrdersQueryHandler.cs
    GetRestaurantNewOrdersQueryValidator.cs
  GetRestaurantActiveOrders/
    GetRestaurantActiveOrdersQuery.cs
    GetRestaurantActiveOrdersQueryHandler.cs
    GetRestaurantActiveOrdersQueryValidator.cs
  GetOrderStatus/ (optional)
    GetOrderStatusQuery.cs
    GetOrderStatusQueryHandler.cs
    GetOrderStatusQueryValidator.cs
  Interfaces/
    IRestaurantQuery.cs (contextual)
    IOrderQuery.cs (optional, forward-looking)
```

Rationale: Consolidated common DTOs & helpers to reduce duplication and provide clear ownership for pagination + status logic.

## 🧱 DTO Design (Draft Shapes – conceptual only)
1. OrderDetailsDto
   - OrderId, CustomerId, RestaurantId, Status, CreatedAtUtc, PlacedAtUtc?, AcceptedAtUtc?, PreparedAtUtc?, ReadyAtUtc?, DeliveredAtUtc?, CancelledAtUtc?, RejectedAtUtc?
   - Pricing: Subtotal, DiscountsTotal, Tax, DeliveryFee, ServiceFee, Total
   - Payment: PaymentStatus, PaymentMethodType
   - Items: List<OrderItemDto>
     - ItemId, MenuItemId, Name, Quantity, UnitPrice, LineTotal
     - Customizations: List<OrderItemCustomizationDto> (GroupName, OptionName, PriceDelta)
2. OrderSummaryDto (for lists)
   - OrderId, Status, CreatedAtUtc, LatestStatusTimestamp, RestaurantId (for customer list) or CustomerId (for restaurant lists), Total, ItemCount
3. OrderStatusDto (optional lean)
   - OrderId, Status, LastUpdatedUtc
4. Paging container: PaginatedList<OrderSummaryDto> (reuse existing model)
5. Consider minimal columns for performance in list queries (avoid heavy item join unless required)

## 🔐 Authorization & Access Constraints (Refined)
Strategy aligns with existing `AuthorizationBehaviour` & contextual interfaces.

| Query | Context Interface | Attribute | Enforcement Detail |
|-------|-------------------|-----------|--------------------|
| GetOrderById | IOrderQuery & (internally also needs RestaurantId -> join/lookup) | None or future `[Authorize(Policy=...)]` | Post-fetch: if requester is order customer OR has restaurant staff permission for order's restaurant; else return NotFound (avoid existence leak) |
| GetCustomerRecentOrders | (None) – uses current user claims | `[Authorize]` (implicit – may rely on controller-level) | Enforce current user Id == requested customer (if parameter optional) |
| GetRestaurantNewOrders | IRestaurantQuery | `[Authorize(Policy = Policies.MustBeRestaurantStaff)]` | Policy ensures staff; query filtered by RestaurantId |
| GetRestaurantActiveOrders | IRestaurantQuery | `[Authorize(Policy = Policies.MustBeRestaurantStaff)]` | Same as above |
| GetOrderStatus (optional) | IOrderQuery | None initially | Same access check as GetOrderById but lean payload |

Enhancement (Deferred): Introduce `Policies.MustBeOrderOwner` + order permissions when/if order-scoped claims added to login claims generation. Plan documents but does not implement in Phase 3.2.

## ✅ Validation Rules (FluentValidation)
Common:
- Guid identifiers: not empty
- Paging: PageNumber >= 1, PageSize within allowed max (e.g., <= 100)
Specific:
- GetCustomerRecentOrders: CustomerId must match current user (if provided explicitly) or omit parameter and infer from claims
- Restaurant queries: RestaurantId not empty
- OrderStatus: OrderId not empty

## 🧠 Handler Responsibilities (Per Guidelines + Refinements)
1. Validate inputs (pipeline will call validator first).
2. Build SQL via constant `const string` or local `ReadOnlySpan<char>` (string constants for now) with explicit column selection & aliases.
3. Use `CommandDefinition` with cancellation token.
4. For paged queries call `DapperPagination.QueryPageAsync`.
5. For detail query execute:
  - Order row (single) – if null => NotFound.
  - Items + customizations – second query; group by OrderItemId.
6. Authorization post-fetch for customer-owned queries (compare `order.CustomerId` vs current user id claim).
7. Map to DTOs, then wrap in `Result.Success`.
8. Log at `Information` level (query name + key parameters + row counts).
9. Return domain-neutral DTOs (no entities / value objects directly unless value object types already used in other DTOs like `OrderId`).

## 🗃️ SQL Design Strategy
General considerations:
- Separate detail vs list queries to avoid N+1
- Use explicit column lists (no SELECT *) for stability
- Alias columns to map directly to DTO property names
- Convert enums via underlying value or textual mapping
- Use window functions or COUNT OVER for paging total when efficient

GetOrderById:
- Query 1: Order base + aggregates (status timestamps, pricing fields)
- Query 2: Order line items with customization options (LEFT JOIN)
Option A: Single multi-join result shaped then manual grouping in handler
Option B: Two queries (main + items) – simpler logic, acceptable if limited frequency
Index usage: WHERE OrderId = @OrderId → needs PK/clustered index already

GetCustomerRecentOrders:
- Base SQL: FROM Orders WHERE CustomerId = @CustomerId ORDER BY PlacementTimestamp DESC
- Page query adds OFFSET/FETCH (SQL Server) or LIMIT/OFFSET (if PostgreSQL – confirm actual provider from infrastructure; assumption: PostgreSQL due to quoted identifiers & casting patterns observed in existing Dapper queries) -> Use PostgreSQL syntax: `LIMIT @PageSize OFFSET @Offset`.
- Total count separate query for simplicity (optimize to window function later if required).
- ItemCount via subquery `(SELECT COUNT(*) FROM "OrderItems" oi WHERE oi."OrderId" = o."Id") AS "ItemCount"`.

GetRestaurantNewOrders:
- WHERE o."RestaurantId" = @RestaurantId AND o."Status" = @PlacedStatus
- ORDER BY o."PlacementTimestamp" ASC (dashboard freshness) – confirm UX; configurable constant.

GetRestaurantActiveOrders:
- WHERE o."RestaurantId" = @RestaurantId AND o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery')
- ORDER BY CASE to force status priority THEN PlacementTimestamp.

GetOrderStatus:
- Lean select: `SELECT o."Id" AS "OrderId", o."Status", o."LastUpdateTimestamp" FROM "Orders" o WHERE o."Id" = @OrderId`
- Optional ETag (deferred) – Document future.

GetOrderStatus:
- Minimal select (Status, relevant last timestamp) – small, low-latency
- Opportunity to add ETag (hash of status+timestamp) for caching later

Parameterization:
- Avoid IN (@Statuses) pitfalls; pass table-valued param or string split; simpler: explicit list in SQL literal since set is fixed and safe (hard-coded allowed statuses).

## 📈 Performance & Indexing Notes (Refined)
- Verify DB engine: existing SQL (quoted identifiers + `::text` casting) indicates PostgreSQL.
- Index Recommendations (PostgreSQL syntax):
  - `CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_customer_placement ON "Orders" ("CustomerId", "PlacementTimestamp" DESC);`
  - `CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_restaurant_status_placement ON "Orders" ("RestaurantId", "Status", "PlacementTimestamp" DESC);`
  - (Optional) Partial index for active statuses: `CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_restaurant_active ON "Orders" ("RestaurantId", "PlacementTimestamp") WHERE "Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery');`
- Consider adding computed or maintained `ItemsCount` column later; initial subquery acceptable (benchmark threshold > p99 latency target 50ms).

## 🧪 Testing Strategy (Refined)
Unit Tests:
- Validators reject invalid paging / empty IDs
- Handlers return NotFound for non-existent OrderId
- Authorization edge (if in handler) returns forbidden when mismatched user
Functional Tests:
- Seed orders with varied statuses; assert paging correctness (first page, last page, empty page)
- Ordering stability (two orders same timestamp – tie-breaker by Id)
- GetOrderById includes correct item/customization counts
- Active vs New sets contain expected statuses only
Performance (optional micro-benchmark):
- Ensure single DB round-trip per handler (except maybe grouping detail query)
Negative Cases:
- Deleted / soft-deleted orders (if soft-delete column—ensure WHERE Excluded = 0)
Flakiness Avoidance:
- Freeze timestamps or use deterministic creation order in test fixtures

## 🧩 Reuse & Cross-Cutting (Refined)
- `DapperPagination` centralizes count + page execution; returns `PaginatedList<T>`
- `OrderQueryConstants` prevents duplication of status arrays and priority ordering
- Logging helper: possibly `QueryLoggingExtensions.LogQueryResult(logger, queryName, rowCount, elapsed)` (optional – can defer)
- Monetary precision: confirm decimal scale in schema (e.g., numeric(18,2)) – ensure no implicit float casts in SQL

## 🛡️ Security / Multi-Tenancy
- Guard leakage: If unauthorized to see an order, respond NotFound (configurable)
- Validate RestaurantId association with Order for staff queries (WHERE RestaurantId = @RestaurantId)
- No direct exposure of internal IDs beyond defined DTOs (OK)

## 🚦 Failure & Edge Handling
- NotFound: Return Result.Failure with NotFound error (consistent with existing pattern)
- Empty lists: Return PaginatedList with empty Items and TotalCount 0
- Large page numbers beyond range: return empty page (do not throw)
- Cancellation: Respect token before/after DB call
- DB exceptions: Let pipeline behavior translate (logging + generic error)

## 📚 Documentation & Comments
- Each handler XML summary: Purpose, performance note, status set definition reference
- SQL preceded by comment block (/* Purpose: ... */) for maintainability
- Add OrderStatusSet constants in a static class for reuse (e.g., ActiveStatuses array)

## 🏷️ Naming Conventions (Confirm)
- Query record: GetOrderByIdQuery
- Handler: GetOrderByIdQueryHandler
- Validator: GetOrderByIdQueryValidator
- Response Types: If wrapping, define response record (e.g., GetOrderByIdResponse(OrderDetailsDto Order)) OR return DTO directly; follow existing project convention (inspect other features); assume response wrapper for consistency.

## 🌐 Web Layer Endpoint Mapping (Refined)
| Endpoint | Query | Auth Attribute (Controller/Action) | Notes |
|----------|-------|-------------------------------------|-------|
| GET `/api/orders/{orderId}` | GetOrderByIdQuery | `[Authorize]` | Handler enforces ownership/staff; returns 404 if unauthorized |
| GET `/api/orders/my` | GetCustomerRecentOrdersQuery | `[Authorize]` | CustomerId inferred from claims; optional explicit parameter rejected if mismatch |
| GET `/api/restaurants/{restaurantId}/orders/new` | GetRestaurantNewOrdersQuery | `[Authorize(Policy=Policies.MustBeRestaurantStaff)]` | Query implements `IRestaurantQuery` |
| GET `/api/restaurants/{restaurantId}/orders/active` | GetRestaurantActiveOrdersQuery | `[Authorize(Policy=Policies.MustBeRestaurantStaff)]` | Same pattern |
| GET `/api/orders/{orderId}/status` | GetOrderStatusQuery | `[Authorize]` | Same ownership/staff guard; lightweight payload |

Result translation: existing middleware/pipeline already handles `Result<T>` => HTTP mapping (verify before implementation).

## 🧵 Concurrency & Consistency Considerations
- Reads are eventually consistent with writes (after transaction commit)
- No need for explicit transaction for queries
- Potential minor staleness acceptable; if strong consistency needed for status polling, optional (READ COMMITTED SNAPSHOT) assumption

## 🔄 Optional Optimizations (Future)
- Caching OrderStatusQuery results with short TTL (Redis)
- Precomputed materialized view for active orders per restaurant
- Add search/filter (date range, status filter expansion)
- Replace OFFSET paging with keyset paging for large datasets (later)

## 🧩 Optional OrderStatusQuery Justification
- Reduces payload vs full details query
- Basis for client polling fallback when SignalR not connected
- Cheaper index-only retrieval if covering index (OrderId, Status, UpdatedAt)

## 🧪 Test Data Builders
- Create OrderTestDataBuilder for seeding multiple statuses
- Utility to bulk insert orders + line items for performance tests

## 🛠️ Implementation Sequence (Refined)
1. Schema & domain field audit (Order aggregate + DB schema doc) – confirm column names for timestamps, pricing, payment, status.
2. Introduce supporting types: `IRestaurantQuery`, `IOrderQuery` (optional), `OrderQueryConstants`, `DapperPagination` helper.
3. Add DTOs (`OrderSummaryDto`, `OrderDetailsDto`, `OrderItemDto`, `OrderItemCustomizationDto`, `OrderStatusDto`).
4. Implement & test `DapperPagination.QueryPageAsync` (unit: simulate connection via integration test or mock IDbConnection with in-memory DB – choose integration if feasible).
5. Implement `GetOrderById` + validator + tests (success, not found, unauthorized access).
6. Implement `GetCustomerRecentOrders` + validator + tests (paging boundaries, ownership check, empty result, page beyond range).
7. Implement `GetRestaurantNewOrders` + validator + tests.
8. Implement `GetRestaurantActiveOrders` + validator + tests.
9. (Optional) Implement `GetOrderStatus` (can share part of SQL from GetOrderById) + tests.
10. Wire endpoints in Web layer controller; add integration/functional tests for each endpoint (HTTP path + auth + response schema).
11. Add migration for recommended indexes (separate migration file) – mark optional if existing indexes suffice.
12. Documentation update in `Application_Layer_Guidelines.md` referencing new pagination helper for Dapper queries.
13. Quality gates: build, run unit & functional tests, review SQL for injection (parameterization check).

### ✅ Step 1 Output: Schema & Domain Field Audit (Completed)
Notable confirmations & decisions driving subsequent query implementations:

1. Core Order Columns (DB vs Domain Names):
  - Id (PK) → maps to `OrderId` (Value Object) but queries will project as `Guid` and wrap if needed at API boundary.
  - OrderNumber (unique) – include in both detail & list summaries for operator usability.
  - Status (string enum) – enum names exactly: AwaitingPayment, Placed, Accepted, Preparing, ReadyForDelivery, Delivered, Cancelled, Rejected. Active set = {Placed, Accepted, Preparing, ReadyForDelivery}. New orders = Status = Placed. Payment pending state name is `AwaitingPayment` (not PendingPayment).
  - PlacementTimestamp, LastUpdateTimestamp, EstimatedDeliveryTime, ActualDeliveryTime – use these names verbatim in SQL (timestamp columns). No CreatedAt separate field; PlacementTimestamp is the canonical creation/placement marker.

2. Financial Columns (Money VO pattern: <Name>_Amount / <Name>_Currency):
  - Subtotal_Amount/Currency, DiscountAmount_Amount/Currency, DeliveryFee_Amount/Currency, TipAmount_Amount/Currency, TaxAmount_Amount/Currency, TotalAmount_Amount/Currency.
  - Plan: For list queries return only TotalAmount (Amount + Currency) plus ItemCount. For detail query return full breakdown (Subtotal, DiscountAmount, DeliveryFee, TipAmount, TaxAmount, TotalAmount). Currency expected consistent across all; do not recompute totals in SQL (trust stored values). Future optimization: include only Amount fields if UI standardizes single currency; keep both now for correctness.

3. Address Snapshot Columns (DeliveryAddress_*):
  - For detail view we likely include structured address (Street, City, State, Country, ZipCode). For list queries omit to reduce payload; optionally return City for future filtering (defer).

4. Coupon / TeamCart References:
  - AppliedCouponId, SourceTeamCartId nullable; include in detail DTO (nullable Guid). Omit in list summary initial version to keep summary lean.

5. Payment / Transactions:
  - PaymentTransactions table separate; we will NOT join in initial queries (except possibly future status-specific enrichment). For detail query, we currently omit transactions (explicit decision) to keep scope small; can add separate `GetOrderPaymentsQuery` later.
  - Payment success/failure affects Status transitions; no separate PaymentStatus column on Orders table; thus `OrderStatus` alone sufficient for queries (lean OrderStatus query returns Status + LastUpdateTimestamp + EstimatedDeliveryTime maybe for UI progress bar).

6. OrderItems & Customizations:
  - OrderItems table: fields for snapshot base price, quantity, SelectedCustomizations (jsonb), LineItemTotal. Domain also tracks customization list as value objects.
  - For detail query we will parse `SelectedCustomizations` JSON into `OrderItemCustomizationDto` components (fields: GroupName, ChoiceName, PriceAdjustmentAmount, PriceAdjustmentCurrency). This requires either: (a) simple JSON deserialization model; (b) leaving raw JSON string initially. Decision: implement typed deserialization (adds DTO + safe fallback if JSON parse fails -> skip customizations) – document in plan.
  - For summary queries we do not join OrderItems; we compute ItemCount via subquery COUNT(*).

7. Timestamps & Sorting:
  - Use PlacementTimestamp for chronological ordering in all list queries.
  - LastUpdateTimestamp used only in detail & status queries; not primary sort key for active list to preserve intuitive sequence of incoming orders.

8. Enum / Status Mapping:
  - Domain `OrderStatus` enum names align 1:1 with persisted string values. No mapping table needed. We will expose status string directly (API clients already consume similar pattern in command results).

9. Monetary Integrity:
  - Domain code enforces `TotalAmount = Subtotal - DiscountAmount + DeliveryFee + TipAmount + TaxAmount`. Queries will not re-derive; they trust persisted numbers. Potential future cross-check health query (not in scope).

10. Pending Edge Cases:
  - AwaitingPayment orders should be excluded from restaurant "new" and "active" queries (active set purposely starts at Placed) – confirm with product; implemented as such.
  - Cancelled or Rejected orders not in active set; they appear only in detail or dedicated history queries (future).

11. JSON Customizations Structure (Assumption until schema sample confirmed):
  - Expected shape: array of objects containing group name, choice name, and price adjustment { groupName, choiceName, priceAdjustment: { amount, currency } }. Provide fallback mapping logic in implementation plan step 5.

12. Index Alignment:
  - Suggested indexes (customer + placement, restaurant + status + placement) fully align with chosen filter/sort columns; no additional computed columns required for Phase 3.2.

These audit outcomes will directly drive column aliasing and DTO field inclusion in subsequent steps. No schema gaps block implementation.

## 📝 Migration / Database Adjustments (If Needed)
- Add/verify indexes (script separate)
- If missing ItemCount optimization, optionally add computed or maintained column later (defer)

## 🧪 Quality Gates Before Merge
- Build passes
- Unit & functional tests for all new queries green
- SQL reviewed for injection safety (parameterization)
- DTO backwards compatible (new endpoints only)

## 🧭 Open Decisions to Confirm (Before Coding)
1. Proceed now with `IOrderQuery` or defer until order-specific policies exist? (Recommendation: Add now for forward compatibility.)
2. Include lean `GetOrderStatusQuery` in Phase 3.2 or schedule for Phase 3.2a with real-time fallback integration? (Recommendation: Include – low effort, high utility.)
3. Maximum allowed `PageSize` (Proposed constant: 100) – enforce in validators.
4. Monetary field naming verification (align with actual DB: e.g., `SubtotalAmount`, `TaxAmount`, etc.) – adjust DTO property names if mismatch or introduce mapping alias.
5. Index migration included in same PR vs separate PR? (Recommendation: Separate PR if migration process is heavy in CI.)
6. Should list queries expose `OrderNumber` for quick human reference? (Recommendation: Yes – add to `OrderSummaryDto`.)

---

Refined plan above integrates current repository patterns, introduces necessary Dapper pagination + contextual query abstractions, and enumerates open decisions before coding.

Let me know if you’d like me to proceed translating this outline into skeleton files or adjust any assumptions.