
# Order Aggregate

## Aggregate Documentation: `Order`

* **Version:** 2.1
* **Last Updated:** 2025-07-22
* **Source File:** `src/Domain/OrderAggregate/Order.cs`

### 1. Overview

**Description:**
Represents a customer's confirmed request for items from a restaurant. It is a transactional record of a purchase. The aggregate manages the complete order lifecycle from placement through fulfillment, including status transitions and payment processing. Financial calculations and coupon validations are handled by dedicated domain services before an order is created.

**Core Responsibilities:**

* Manages the lifecycle of customer orders from creation to completion.
* Acts as the transactional boundary for all order-related operations.
* Enforces business rules for order status transitions.
* Tracks payment status through the `PaymentTransaction` child entity.
* Handles payment status updates driven by external webhooks.

### 2. Structure

* **Aggregate Root:** `Order`
* **Key Child Entities:**
  * `OrderItem`: Represents individual items in the order with snapshot pricing and customizations.
  * `PaymentTransaction`: Tracks payment attempts and their outcomes.
* **Key Value Objects:**
  * `OrderId`: Strongly-typed identifier for the aggregate.
  * `DeliveryAddress`: Snapshot of the customer's delivery address at the time of the order.
  * `OrderItemCustomization`: Snapshot of customization choices and their pricing.
  * `Money`: Represents all monetary values (subtotal, total, fees, etc.).

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Methods)

The `Order` aggregate provides two factory methods for its creation, catering to different scenarios. Both methods require pre-calculated financial values, enforcing a separation of concerns where financial logic is handled by a dedicated domain service.

##### Factory 1: `Create` (Standard Flow)
This method is used for standard single-payment orders (e.g., online credit card or Cash on Delivery). It internally creates the initial `PaymentTransaction`.

```csharp
public static Result<Order> Create(
    UserId customerId,
    RestaurantId restaurantId,
    DeliveryAddress deliveryAddress,
    List<OrderItem> orderItems,
    string specialInstructions,
    Money subtotal,
    Money discountAmount,
    Money deliveryFee,
    Money tipAmount,
    Money taxAmount,
    Money totalAmount, 
    PaymentMethodType paymentMethodType,
    CouponId? appliedCouponId,
    string? paymentGatewayReferenceId = null,
    TeamCartId? sourceTeamCartId = null,
    DateTime? timestamp = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `paymentMethodType` | `PaymentMethodType` | The method of payment (e.g., `CreditCard`, `CashOnDelivery`). |
| `paymentGatewayReferenceId` | `string?` | The payment gateway's transaction ID. Required for online payment types. |
| *... (other parameters as before)* | | |


##### Factory 2: `Create` (Trusted Process Flow)
This method is designed for trusted internal processes, like converting a `TeamCart` into an order, where payment transactions and the initial status are pre-determined.

```csharp
public static Result<Order> Create(
    UserId customerId,
    RestaurantId restaurantId,
    DeliveryAddress deliveryAddress,
    List<OrderItem> orderItems,
    string specialInstructions,
    Money subtotal,
    Money discountAmount,
    Money deliveryFee,
    Money tipAmount,
    Money taxAmount,
    Money totalAmount, 
    List<PaymentTransaction> paymentTransactions,
    CouponId? appliedCouponId,
    OrderStatus initialStatus,
    TeamCartId? sourceTeamCartId = null,
    DateTime? timestamp = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `paymentTransactions`| `List<PaymentTransaction>` | A pre-built list of payment transactions. |
| `initialStatus` | `OrderStatus` | The pre-determined initial status of the order. |
| *... (other parameters as before)* | | |

**Validation Rules & Potential Errors:**

* At least one order item is required (`OrderErrors.OrderItemRequired`).
* The provided financial totals must be consistent (`OrderErrors.FinancialMismatch`).
* The total amount cannot be negative (`OrderErrors.NegativeTotalAmount`).
* A `paymentGatewayReferenceId` is required for online payments (`OrderErrors.PaymentGatewayReferenceIdRequired`).
* The total of payment transactions must match the order's total amount (`OrderErrors.PaymentMismatch`).

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result Accept(DateTime estimatedDeliveryTime, ...)` | Restaurant accepts the order. | Order must be in `Placed` status. | `OrderErrors.InvalidOrderStatusForAccept` |
| `Result Reject(DateTime? timestamp = null)` | Restaurant rejects the order. | Order must be in `Placed` status. | `OrderErrors.InvalidStatusForReject` |
| `Result Cancel(DateTime? timestamp = null)` | Cancels the order. | Order must be in `Placed`, `Accepted`, `Preparing`, or `ReadyForDelivery` status. | `OrderErrors.InvalidOrderStatusForCancel` |
| `Result MarkAsPreparing(DateTime? timestamp = null)` | Marks the order as being prepared. | Order must be in `Accepted` status. | `OrderErrors.InvalidOrderStatusForPreparing` |
| `Result MarkAsReadyForDelivery(DateTime? timestamp = null)`| Marks the order as ready for delivery. | Order must be in `Preparing` status. | `OrderErrors.InvalidOrderStatusForReadyForDelivery` |
| `Result MarkAsDelivered(DateTime? timestamp = null)` | Marks the order as delivered. | Order must be in `ReadyForDelivery` status. | `OrderErrors.InvalidOrderStatusForDelivered` |
| `Result RecordPaymentSuccess(string paymentGatewayReferenceId, ...)` | Confirms a successful payment. | Order must be in `AwaitingPayment` status. | `OrderErrors.InvalidStatusForPaymentConfirmation`, `OrderErrors.PaymentTransactionNotFound` |
| `Result RecordPaymentFailure(string paymentGatewayReferenceId, ...)` | Marks a payment as failed and cancels the order. | Order must be in `AwaitingPayment` status. | `OrderErrors.InvalidStatusForPaymentConfirmation`, `OrderErrors.PaymentTransactionNotFound` |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `OrderId` | The unique identifier of the order. |
| `OrderNumber` | `string` | Human-readable order number. |
| `Status` | `OrderStatus` | Current status in the order lifecycle. |
| `PlacementTimestamp` | `DateTime` | When the order was placed (UTC). |
| `LastUpdateTimestamp` | `DateTime` | When the order was last modified (UTC). |
| `EstimatedDeliveryTime` | `DateTime?` | The estimated delivery time from the restaurant. |
| `ActualDeliveryTime` | `DateTime?` | The actual time the order was delivered. |
| `Subtotal` | `Money` | The subtotal of the order. |
| `DiscountAmount` | `Money` | The discount applied to the order. |
| `DeliveryFee` | `Money` | The delivery fee for the order. |
| `TipAmount` | `Money` | The tip for the order. |
| `TaxAmount` | `Money` | The tax amount for the order. |
| `TotalAmount` | `Money` | The final amount to be charged. |
| `CustomerId` | `UserId` | The customer who placed the order. |
| `RestaurantId` | `RestaurantId` | The restaurant fulfilling the order. |
| `AppliedCouponId` | `CouponId?` | The ID of the coupon applied. |
| `OrderItems` | `IReadOnlyList<OrderItem>` | Read-only collection of ordered items. |
| `PaymentTransactions` | `IReadOnlyList<PaymentTransaction>` | Read-only collection of payment attempts. The `PaymentGatewayReferenceId` is stored here. |

#### 4.2. Public Query Methods

This aggregate does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `OrderCreated` | During the `Create` factory method. | Signals that a new order has been successfully created. |
| `OrderPaymentSucceeded` | After a successful call to `RecordPaymentSuccess`. | Signals that the payment for the order has succeeded. |
| `OrderPaymentFailed` | After a successful call to `RecordPaymentFailure`. | Signals that the payment for the order has failed. |
| `OrderAccepted` | After a successful call to `Accept`. | Signals that the restaurant has accepted the order. |
| `OrderRejected` | After a successful call to `Reject`. | Signals that the restaurant has rejected the order. |
| `OrderCancelled` | After a successful call to `Cancel` or `RecordPaymentFailure`. | Signals that the order has been cancelled. |
| `OrderPreparing` | After a successful call to `MarkAsPreparing`. | Signals that order preparation has begun. |
| `OrderReadyForDelivery` | After a successful call to `MarkAsReadyForDelivery`. | Signals that the order is ready for delivery. |
| `OrderDelivered` | After a successful call to `MarkAsDelivered`. | Signals that the order has been delivered. |

### 6. Order Status Lifecycle

The `OrderStatus` enum defines the following valid statuses and their transitions, now including a two-phase payment flow.

| Status | Description | Valid Transitions |
| :--- | :--- | :--- |
| `AwaitingPayment` | Initial status for online payments, awaiting confirmation. | → `Placed`, `Cancelled` |
| `Placed` | Initial status for cash-on-delivery or confirmed online payments. | → `Accepted`, `Rejected`, `Cancelled` |
| `Accepted` | Restaurant has accepted the order. | → `Preparing`, `Cancelled` |
| `Preparing` | Order is being prepared. | → `ReadyForDelivery`, `Cancelled` |
| `ReadyForDelivery` | Order is ready for pickup/delivery. | → `Delivered`, `Cancelled` |
| `Delivered` | Order has been completed. | (Terminal state) |
| `Cancelled` | Order was cancelled (by user, restaurant, or due to payment failure). | (Terminal state) |
| `Rejected` | Order was rejected by the restaurant. | (Terminal state) |

### 7. Financial Calculations & Integrity

The `Order` aggregate no longer performs financial calculations (e.g., subtotal, discounts, final total). This responsibility has been moved to the `OrderFinancialService` to adhere to the Single Responsibility Principle.

The `Order.Create` factory method now enforces financial integrity by validating that the sum of the provided financial components (`subtotal`, `discountAmount`, `deliveryFee`, `tipAmount`, `taxAmount`) equals the `totalAmount`. This ensures that the order is created in a consistent and valid state.

### 8. Coupon Application

Coupon application logic, including validation and discount calculation, has been removed from the `Order` aggregate and is now handled exclusively by the `OrderFinancialService`. The `Order` aggregate now only stores the `AppliedCouponId` as a reference.
