# Order Aggregate

## Aggregate Documentation: `Order`

* **Version:** 1.0
* **Last Updated:** 2024-12-13
* **Source File:** `src/Domain/OrderAggregate/Order.cs`

### 1. Overview

**Description:**
Represents a customer's confirmed request for items from a restaurant. It is a transactional, immutable record of a purchase, ensuring historical accuracy. The aggregate manages the complete order lifecycle from placement through fulfillment, including financial calculations, status transitions, and payment processing.

**Core Responsibilities:**

* Manages the lifecycle of customer orders from placement to completion
* Acts as the transactional boundary for all order-related operations
* Enforces business rules for order status transitions and financial calculations
* Enforces business rules for coupon application and payment processing

### 2. Structure

* **Aggregate Root:** `Order`
* **Key Child Entities:**
  * `OrderItem`: Represents individual items in the order with snapshot pricing and customizations
  * `PaymentTransaction`: Tracks payment attempts and their outcomes
* **Key Value Objects:**
  * `OrderId`: Strongly-typed identifier for the aggregate
  * `DeliveryAddress`: Snapshot of customer's delivery address at order time
  * `OrderItemCustomization`: Snapshot of customization choices and pricing
  * `Money`: Represents all monetary amounts (subtotal, total, fees, etc.)

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create an `Order` is through its static factory method.

```csharp
public static Result<Order> Create(
    UserId customerId,
    RestaurantId restaurantId,
    DeliveryAddress deliveryAddress,
    List<OrderItem> orderItems,
    string specialInstructions,
    Money? discountAmount = null,
    Money? deliveryFee = null,
    Money? tipAmount = null,
    Money? taxAmount = null,
    List<CouponId>? appliedCouponIds = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `customerId` | `UserId` | The customer placing the order |
| `restaurantId` | `RestaurantId` | The restaurant fulfilling the order |
| `deliveryAddress` | `DeliveryAddress` | Snapshot of delivery address |
| `orderItems` | `List<OrderItem>` | List of items being ordered (at least one required) |
| `specialInstructions` | `string` | Customer's special instructions |
| `discountAmount` | `Money?` | Optional discount amount from coupons |
| `deliveryFee` | `Money?` | Optional delivery fee |
| `tipAmount` | `Money?` | Optional tip amount |
| `taxAmount` | `Money?` | Optional tax amount |
| `appliedCouponIds` | `List<CouponId>?` | Optional list of applied coupon IDs |

**Validation Rules & Potential Errors:**

* At least one order item is required. (Returns `OrderErrors.OrderItemRequired`)
* Total amount cannot be negative. (Returns `OrderErrors.NegativeTotalAmount`)
* Order is created with status `Placed` and current timestamp
* Order number is auto-generated in format `ORD-YYYYMMDD-HHMMSS-XXXX`

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result Accept(DateTime estimatedDeliveryTime)` | Restaurant accepts the order | Order must be in `Placed` status | `OrderErrors.InvalidOrderStatusForAccept` |
| `Result Reject()` | Restaurant rejects the order | Order must be in `Placed` status | `OrderErrors.InvalidStatusForReject` |
| `Result Cancel()` | Cancels the order | Order must be in `Placed` or `Accepted` status | `OrderErrors.InvalidOrderStatusForCancel` |
| `Result AddPaymentAttempt(PaymentTransaction payment)` | Adds a payment transaction | None - always succeeds with valid input | None |
| `Result MarkAsPaid(PaymentTransactionId paymentTransactionId)` | Marks payment as successful | Payment transaction must exist | `OrderErrors.PaymentNotFound` |
| `Result ApplyCoupon(CouponId, CouponValue, AppliesTo, Money?)` | Applies a coupon discount | Order must be `Placed`, no existing coupon, meets minimum amount | `OrderErrors.CouponCannotBeAppliedToOrderStatus`, `OrderErrors.CouponAlreadyApplied`, `OrderErrors.CouponNotApplicable` |
| `Result RemoveCoupon()` | Removes applied coupon | None - always succeeds | None |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `OrderId` | The unique identifier of the order |
| `OrderNumber` | `string` | Human-readable order number |
| `Status` | `OrderStatus` | Current status in the order lifecycle |
| `PlacementTimestamp` | `DateTime` | When the order was placed (UTC) |
| `LastUpdateTimestamp` | `DateTime` | When the order was last modified (UTC) |
| `EstimatedDeliveryTime` | `DateTime?` | Estimated delivery time (set when accepted) |
| `SpecialInstructions` | `string` | Customer's special instructions |
| `DeliveryAddress` | `DeliveryAddress` | Snapshot of delivery address |
| `Subtotal` | `Money` | Sum of all line items before discounts |
| `DiscountAmount` | `Money` | Total discount from applied coupons |
| `DeliveryFee` | `Money` | Delivery fee charged |
| `TipAmount` | `Money` | Tip amount |
| `TaxAmount` | `Money` | Tax amount (if applicable) |
| `TotalAmount` | `Money` | Final amount to be charged |
| `CustomerId` | `UserId` | The customer who placed the order |
| `RestaurantId` | `RestaurantId` | The restaurant fulfilling the order |
| `OrderItems` | `IReadOnlyList<OrderItem>` | Read-only collection of ordered items |
| `PaymentTransactions` | `IReadOnlyList<PaymentTransaction>` | Read-only collection of payment attempts |
| `AppliedCouponIds` | `IReadOnlyList<CouponId>` | Read-only collection of applied coupon IDs |

#### 4.2. Public Query Methods

This aggregate does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `OrderCreated` | During the `Create` factory method | Signals that a new order has been successfully placed |
| `OrderAccepted` | After a successful call to `Accept` | Signals that the restaurant has accepted the order |
| `OrderRejected` | After a successful call to `Reject` | Signals that the restaurant has rejected the order |
| `OrderCancelled` | After a successful call to `Cancel` | Signals that the order has been cancelled |
| `OrderPaid` | After a successful call to `MarkAsPaid` | Signals that payment has been successfully processed |
| `OrderPreparing` | When order status changes to preparing | Signals that order preparation has begun |

### 6. Order Status Lifecycle

The `OrderStatus` enum defines the following valid statuses and their transitions:

| Status | Description | Valid Transitions |
| :--- | :--- | :--- |
| `Placed` | Initial status when order is created | → Accepted, Rejected, Cancelled |
| `Accepted` | Restaurant has accepted the order | → Preparing, Cancelled |
| `Preparing` | Order is being prepared | → ReadyForDelivery |
| `ReadyForDelivery` | Order is ready for pickup/delivery | → Delivered |
| `Delivered` | Order has been completed | (Terminal state) |
| `Cancelled` | Order was cancelled | (Terminal state) |
| `Rejected` | Order was rejected by restaurant | (Terminal state) |

### 7. Financial Calculations

The aggregate maintains strict financial integrity through the following invariants:

* **Subtotal** = Sum of all `OrderItem.LineItemTotal`s
* **TotalAmount** = `Subtotal - DiscountAmount + TaxAmount + DeliveryFee + TipAmount`
* **LineItemTotal** (per item) = `(BasePrice + CustomizationAdjustments) × Quantity`
* **Discount** cannot exceed the subtotal amount
* **TotalAmount** cannot be negative

### 8. Coupon Application Rules

* Only one coupon can be applied per order
* Coupons can only be applied to orders in `Placed` status
* Discount calculation varies by coupon type:
  * **Percentage**: Applied to eligible items based on scope
  * **FixedAmount**: Limited to the value of eligible items
  * **FreeItem**: Discount equals the price of one unit of the cheapest matching item
* Coupon scope determines eligible items:
  * **WholeOrder**: Applied to entire subtotal
  * **SpecificItems**: Applied only to matching menu items
  * **SpecificCategories**: Applied only to items in matching categories
