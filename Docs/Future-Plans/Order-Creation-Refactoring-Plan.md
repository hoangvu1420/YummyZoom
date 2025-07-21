# Order Aggregate Refactoring Plan

This document outlines the plan for refactoring the `Order` aggregate within the Domain layer. The goal is to align the aggregate with Domain-Driven Design (DDD) principles, making it an immutable record of a confirmed transaction rather than an entity that performs complex business logic calculations.

## 1. Analysis of the Current `Order` Aggregate

The current implementation of the `Order` aggregate (`src/Domain/OrderAggregate/Order.cs`) is a "fat" aggregate. It combines data persistence with complex business logic, making it difficult to test, maintain, and reason about.

Key issues with the current implementation:

*   **Mutability:** The aggregate's state can be changed after creation through several public methods. This violates the principle that an order, once placed, should be an immutable record.
*   **Mixed Responsibilities:** The aggregate is responsible for:
    *   Calculating its own totals (`RecalculateTotals`).
    *   Applying and removing coupons (`ApplyCoupon`, `RemoveCoupon`), which involves complex validation and calculation logic.
    *   Managing payment attempts (`AddPaymentAttempt`, `MarkAsPaid`).
*   **Complex `Create` Method:** The static `Create` factory method performs calculations and has optional parameters for financial figures, indicating that the calculation logic is spread across different layers.

This design leads to a less robust and more error-prone order creation process.

## 2. Proposed Refactoring of the `Order` Aggregate

The refactoring will focus on separating concerns, moving business logic out of the aggregate and into a stateless domain service, and making the `Order` aggregate a pure, immutable entity.

### 2.1. Remove Business Logic Methods from `Order.cs`

The following public methods, which contain business logic and mutate the order's state, will be **removed** from the `Order` class:

*   `ApplyCoupon()`
*   `RemoveCoupon()`
*   `AddPaymentAttempt()`
*   `MarkAsPaid()`
*   `RecalculateTotals()` (private method)
*   `GetDiscountBaseAmount()` (private method)
*   `GetFreeItemDiscountAmount()` (private method)

The logic contained within these methods will be moved to the new `OrderFinancialService`.

### 2.2. Redesign the `Order.Create` Factory Method

The `Create` factory method will be redesigned to enforce the new immutability principle. It will no longer perform any calculations. Instead, it will act as a final validation gatekeeper, ensuring the integrity of the pre-calculated data it receives.

**New `Order.Create` Signature:**

```csharp
public static Result<Order> Create(
    UserId customerId,
    RestaurantId restaurantId,
    DeliveryAddress deliveryAddress,
    List<OrderItem> orderItems,
    string specialInstructions,
    // --- Required, Pre-Calculated Financial Details ---
    Money subtotal,
    Money discountAmount,
    Money deliveryFee,
    Money tipAmount,
    Money taxAmount,
    Money totalAmount, // The final, expected total
    // --- Required Transaction & Reference Details ---
    List<PaymentTransaction> paymentTransactions,
    CouponId? appliedCouponId,
    TeamCartId? sourceTeamCartId = null)
```

**New `Order.Create` Internal Logic:**

The method's sole responsibility is to perform consistency checks before creating the `Order` instance.

1.  **Financial Integrity Check:** It will calculate a transient total from the provided financial components (`subtotal`, `discountAmount`, etc.) and assert that this calculated total matches the `totalAmount` parameter.
    *   If they do not match, it returns `OrderErrors.FinancialMismatch`.
2.  **Payment Integrity Check:** It will sum the amounts of all `paymentTransactions` and assert that this sum equals the `totalAmount` parameter.
    *   If they do not match, it returns `OrderErrors.PaymentMismatch`.
3.  **Instance Creation:** If all checks pass, it creates and returns the new `Order` instance and raises an `OrderCreated` domain event.

### 2.3. Introduce the `OrderFinancialService`

A new stateless domain service, `OrderFinancialService`, will be created in `src/Domain/Services/`. This service will encapsulate all the financial calculation logic that was removed from the `Order` aggregate.

**File:** `src/Domain/Services/OrderFinancialService.cs`

**Responsibilities:**

*   Provide pure, stateless methods for all order-related financial calculations.
*   Have no dependencies on infrastructure (no repositories, no I/O).

**Public Methods:**

*   `public Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)`
*   `public Result<Money> ValidateAndCalculateDiscount(Coupon coupon, int currentUserUsageCount, IReadOnlyList<OrderItem> orderItems, Money subtotal)`
*   `public Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)`

This service will be used by the Application layer to compute all financial details *before* calling `Order.Create`.

## 3. Considerations and Recommendations

*   **Impact on Other Layers:** This refactoring is confined to the Domain layer, but it is the first step in a larger process. The `CreateOrderCommandHandler` in the Application layer will need to be significantly updated to use the new `OrderFinancialService` and orchestrate the order creation process correctly.
*   **Testing:**
    *   Existing unit tests for the `Order` aggregate must be rewritten to reflect its new role as an immutable data container and validator.
    *   New unit tests must be created for the `OrderFinancialService` to cover all the business logic that was moved into it.
*   **Next Steps:** The immediate next step is to implement the changes described in this document: refactor the `Order` class and create the `OrderFinancialService` class. This will lay the foundation for the subsequent refactoring of the Application layer.
