# Order origin (TeamCart) + payment breakdown in Order APIs

## Goal
Make Order API responses explicitly distinguish:

1) Whether an order originated from a TeamCart, and
2) For TeamCart-originated orders, the split between the portion paid online vs the portion due/paid as Cash on Delivery (COD).

This document is analysis + a concrete implementation plan only (no code changes).

---

## Current state (what the code already supports)

### Domain model already has “origin from TeamCart”
- `Order.SourceTeamCartId` exists in the aggregate (`src/Domain/OrderAggregate/Order.cs`).
- EF maps `SourceTeamCartId` to the `Orders` table (`src/Infrastructure/Persistence/EfCore/Configurations/OrderConfiguration.cs`).
- TeamCart conversion sets it:
  - `TeamCartConversionService.ConvertToOrder(...)` creates the order with `sourceTeamCartId: teamCart.Id` (`src/Domain/Services/TeamCartConversionService.cs`).
- Standard order initiation can also set it:
  - `InitiateOrderCommand` has `Guid? TeamCartId` and passes it through to `Order.Create(... sourceTeamCartId: ...)` (`src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommand.cs`, `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs`).

### TeamCart-originated orders already have the payment split captured (implicitly)
- TeamCart conversion builds *multiple* `PaymentTransaction` rows, one per member payment (`TeamCartConversionService.CreateSucceededPaymentTransactions(...)`).
- Each transaction has:
  - `PaymentMethodType` (converted from member payment method: Online → `CreditCard`, COD → `CashOnDelivery`)
  - `Amount` (Money) and `PaidByUserId`
  - `Status` (forced to `Succeeded` during conversion)

So the database has enough information to compute “online portion” vs “COD portion” by summing `PaymentTransactions.Transaction_Amount` by `PaymentMethodType` (and filtering by `Type='Payment'`, `Status='Succeeded'`).

### Some order detail endpoints already expose `SourceTeamCartId`
- `OrderDetailsDto` includes `Guid? SourceTeamCartId` (`src/Application/Orders/Queries/Common/OrderDtos.cs`).
- Both detail query handlers select it from SQL (`GetOrderByIdQueryHandler`, `GetRestaurantOrderByIdQueryHandler`).

---

## Current gaps (why UI/API cannot distinguish today)

### 1) List endpoints don’t show TeamCart origin at all
The list DTOs used by:
- `GET /api/v1/orders/my` (customer)
- `GET /api/v1/restaurants/{id}/orders/new`
- `GET /api/v1/restaurants/{id}/orders/active`

…return `OrderSummaryDto`, which currently has no `SourceTeamCartId` and no explicit “origin” field (`src/Application/Orders/Queries/Common/OrderDtos.cs`).

### 2) Payment info in Order APIs collapses multi-transaction TeamCart payments to a single “PaymentMethod”
In order detail responses:
- `GetOrderByIdQueryHandler` selects *one* `PaymentMethod` by taking the earliest payment transaction.
- `GetRestaurantOrderByIdQueryHandler` does the same.

In order history responses:
- `OrderHistorySummaryDto` includes `PaymentStatus` + `PaymentMethod`, but the SQL is based on “latest status” + “earliest method”.

For TeamCart-originated orders with mixed member payments:
- “PaymentMethod” becomes misleading (a TeamCart order is not a single-method payment).
- There is no field that exposes the *split* between online and COD portions.

### 3) “Origin” semantics are implicit and inconsistent for clients
Even though `OrderDetailsDto.SourceTeamCartId` exists, clients must infer origin by checking nullability.
That inference is unavailable on the list endpoints and status endpoint (`OrderStatusDto`).

---

## Proposed API contract (minimal + clear)

### Add “origin” fields to all order representations used by APIs
Expose both:
- `Guid? SourceTeamCartId` (already present in details; add to summary/history/status as needed)
- A convenience discriminator such as:
  - `bool IsFromTeamCart` (derived from `SourceTeamCartId != null`), do this.
Rationale: clients can render reliably without hard-coding null checks everywhere, and list endpoints become self-describing.

### Add payment split fields (especially for TeamCart orders)
Expose two amounts (same currency as the order totals):
- `decimal PaidOnlineAmount`
- `decimal CashOnDeliveryAmount`

Recommended definition:
- `PaidOnlineAmount` = sum of succeeded `PaymentTransactions` where `Type='Payment'` and `PaymentMethodType != 'CashOnDelivery'`
- `CashOnDeliveryAmount` = sum of succeeded `PaymentTransactions` where `Type='Payment'` and `PaymentMethodType = 'CashOnDelivery'`

Notes:
- For single-payment online orders awaiting payment, `PaidOnlineAmount` will likely be `0` until webhook success (because the initial transaction is `Pending`). That’s fine if the product requirement is specifically about TeamCart-originated orders (which are created with succeeded transactions during conversion).
- If the UI also needs “amount due online”, that is a separate field and requirement.

---

## Step-by-step implementation plan (follow existing project patterns)

1) **Confirm API contract shape**
   - Decide between `IsFromTeamCart` vs `Origin` (or both).
   - Decide where the payment split must appear:
     - At minimum: `OrderDetailsDto`
     - Recommended: also `OrderSummaryDto` + `OrderHistorySummaryDto` so list screens can show it without a detail call.

2) **Extend DTOs in `src/Application/Orders/Queries/Common/OrderDtos.cs`**
   - Update `OrderSummaryDto` to include `SourceTeamCartId` and (optionally) `IsFromTeamCart`, plus payment split fields if required on list screens.
   - Update `OrderHistorySummaryDto` similarly.
   - Update `OrderDetailsDto` to include `PaidOnlineAmount` and `CashOnDeliveryAmount` (and optionally the explicit origin discriminator).
   - Consider also extending `OrderStatusDto` if status polling screens need origin (otherwise keep it lean).

3) **Update customer order list query**
   - `GetCustomerRecentOrdersQueryHandler`:
     - Add `o."SourceTeamCartId" AS SourceTeamCartId`.
     - Add payment split via either:
       - correlated subqueries per order, or
       - a `LEFT JOIN LATERAL` to an aggregated `PaymentTransactions` subquery.

4) **Update restaurant new/active order list queries**
   - `GetRestaurantNewOrdersQueryHandler` and `GetRestaurantActiveOrdersQueryHandler`:
     - Same additions as customer list: `SourceTeamCartId` + payment split.

5) **Update restaurant order history query**
   - `GetRestaurantOrderHistoryQueryHandler`:
     - Add `SourceTeamCartId`.
     - Add payment split fields.
     - Optionally adjust “PaymentMethod” semantics:
       - keep existing field for backward compatibility, but for TeamCart orders consider returning a special value like `Mixed` (or leave it and let clients use the split fields instead).

6) **Update order detail queries**
   - `GetOrderByIdQueryHandler` and `GetRestaurantOrderByIdQueryHandler`:
     - Add a payment aggregation query (or extend SQL) that returns `PaidOnlineAmount` + `CashOnDeliveryAmount` for the order.
     - Keep existing `PaymentMethod` if needed, but clients should prefer the split fields for TeamCart orders.

7) **Update Web API contract tests and functional tests**
   - Any tests instantiating `OrderSummaryDto`, `OrderHistorySummaryDto`, or `OrderDetailsDto` will need constructor updates (e.g., `tests/Web.ApiContractTests/...`).
   - Prefer adding assertions for the new fields in at least one contract test to prevent regression.

8) **Performance check**
   - For list queries: prefer a single aggregated join over multiple correlated subqueries if the list size is large.
   - Ensure proper filtering: `Type='Payment'` and usually `Status='Succeeded'` for “paid” amounts.

9) **Client rollout guidance**
   - Treat added fields as backward-compatible (additive).
   - Update UI to:
     - render `IsFromTeamCart` / `Origin`
     - show `PaidOnlineAmount` + `CashOnDeliveryAmount` when `IsFromTeamCart` is true (or `SourceTeamCartId != null`).

---

## Acceptance criteria
- Order list endpoints return a clear discriminator showing TeamCart origin.
- Order detail endpoint returns `PaidOnlineAmount` and `CashOnDeliveryAmount` for TeamCart-originated orders (sum equals order total within rounding tolerance).
- Restaurant history endpoint can display the split without relying on a single “PaymentMethod”.

---

## Client rollout guidance (implemented)

### What changed in the API

These changes are additive fields in existing responses (no route changes):

- **Order lists (`OrderSummaryDto`)**
  - Endpoints:
    - `GET /api/v1/orders/my`
    - `GET /api/v1/restaurants/{restaurantId}/orders/new`
    - `GET /api/v1/restaurants/{restaurantId}/orders/active`
  - Added fields per item:
    - `sourceTeamCartId` (nullable UUID)
    - `isFromTeamCart` (boolean)
    - `paidOnlineAmount` (number)
    - `cashOnDeliveryAmount` (number)

- **Restaurant order history (`OrderHistorySummaryDto`)**
  - Endpoint: `GET /api/v1/restaurants/{restaurantId}/orders/history`
  - Added fields per item:
    - `sourceTeamCartId`, `isFromTeamCart`
    - `paidOnlineAmount`, `cashOnDeliveryAmount`

- **Order details (`OrderDetailsDto`)**
  - Endpoints:
    - `GET /api/v1/orders/{orderId}`
    - `GET /api/v1/restaurants/{restaurantId}/orders/{orderId}`
  - Added fields:
    - `isFromTeamCart` (boolean)
    - `paidOnlineAmount` (number)
    - `cashOnDeliveryAmount` (number)
  - Note: `sourceTeamCartId` already existed and remains the canonical origin reference.

- **Order status (`OrderStatusDto`)**
  - Endpoint: `GET /api/v1/orders/{orderId}/status`
  - Added fields:
    - `sourceTeamCartId` (nullable UUID)
    - `isFromTeamCart` (boolean)

### What API docs were updated

- `Docs/API-Documentation/API-Reference/Customer/03-Individual-Orders.md`
- `Docs/API-Documentation/API-Reference/Customer/Workflows/03-Order-Tracking-and-Real-time-Updates.md`
- `Docs/API-Documentation/API-Reference/Restaurant/05-Order-Operations.md`
- `Docs/API-Documentation/API-Reference/Restaurant/Workflows/02-Operational-Order-Lifecycle.md`

### Notes for client teams

- **Origin logic**
  - Use `isFromTeamCart` for display logic; treat `sourceTeamCartId` as the source-of-truth identifier when you need to deep-link or fetch TeamCart context.

- **Payment split semantics**
  - Prefer `paidOnlineAmount` + `cashOnDeliveryAmount` over `paymentMethod` for TeamCart-originated orders (because a TeamCart order can have multiple member transactions).
  - The split fields are derived from **succeeded** payment transactions:
    - Orders that are still `AwaitingPayment` may show `paidOnlineAmount = 0` until the payment webhook is processed.

- **Backward compatibility**
  - These are additive response fields. Most clients will be fine, but strict JSON deserializers that reject unknown fields must be updated to accept the new fields.
