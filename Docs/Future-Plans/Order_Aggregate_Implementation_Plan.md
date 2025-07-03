
# Order Aggregate Implementation Plan

This document outlines the detailed plan for implementing the `Order` aggregate within the YummyZoom Domain Layer. The implementation will strictly adhere to the principles and patterns defined in `Docs/Design/Domain_Design.md` and `Docs/Domain_Layer_Guidelines.md`.

---

## 1. Folder and File Structure

A new folder `OrderAggregate` will be created inside `src/Domain`. The internal structure will mirror existing aggregates:

```
src/Domain/
└── OrderAggregate/
    ├── Entities/
    │   ├── OrderItem.cs
    │   └── PaymentTransaction.cs
    ├── Enums/
    │   ├── OrderStatus.cs
    │   ├── PaymentStatus.cs
    │   └── PaymentTransactionType.cs
    ├── Errors/
    │   └── Order.cs
    ├── Events/
    │   ├── OrderAccepted.cs
    │   ├── OrderCancelled.cs
    │   ├── OrderCreated.cs
    │   ├── OrderPaid.cs
    │   └── OrderRejected.cs
    ├── ValueObjects/
    │   ├── OrderId.cs
    │   ├── OrderItemId.cs
    │   ├── OrderItemCustomization.cs
    │   ├── PaymentTransactionId.cs
    │   └── DeliveryAddress.cs
    └── Order.cs
```

---

## 2. Value Objects Implementation

Value Objects will be immutable and enforce their own validity.

### IDs

* **`OrderId.cs`**: Inherits from `AggregateRootId<Guid>`. It will have a static `CreateUnique()` and a `Create(Guid value)` factory method.
* **`OrderItemId.cs`**: Inherits from `ValueObject`. It will be a wrapper around a `Guid` with `CreateUnique()` and `Create(Guid value)` factory methods.
* **`PaymentTransactionId.cs`**: Inherits from `ValueObject`. Similar structure to `OrderItemId`.

### Snapshot and Composite VOs

* **`DeliveryAddress.cs`**: A `ValueObject` that is a snapshot of the customer's chosen address at the time of order.
  * **Properties**: `Street`, `City`, `State`, `ZipCode`, `Country`.
  * **Factory Method**: `Create(...)` will validate that no property is null or empty.
* **`OrderItemCustomization.cs`**: A `ValueObject` to snapshot the details of a selected customization for an order item.
  * **Properties**: `Snapshot_CustomizationGroupName` (string), `Snapshot_ChoiceName` (string), `Snapshot_ChoicePriceAdjustmentAtOrder` (Money).
  * **Factory Method**: `Create(...)` will validate inputs.

---

## 3. Enumerations

Simple `enum` types will be created to represent the discrete states and types within the aggregate.

* **`OrderStatus.cs`**: `Placed`, `Accepted`, `Preparing`, `ReadyForDelivery`, `Delivered`, `Cancelled`, `Rejected`.
* **`PaymentTransactionType.cs`**: `Payment`, `Refund`.
* **`PaymentStatus.cs`**: `Pending`, `Succeeded`, `Failed`.

---

## 4. Entities Implementation

Entities will be mutable but their state changes will be managed by the `Order` aggregate root.

### `OrderItem.cs`

* **Inheritance**: `Entity<OrderItemId>`.
* **Properties**:
  * `Snapshot_MenuItemID` (MenuItemId)
  * `Snapshot_ItemName` (string)
  * `Snapshot_BasePriceAtOrder` (Money)
  * `Quantity` (int)
  * `LineItemTotal` (Money)
  * A private `List<OrderItemCustomization>` with a public `IReadOnlyList<OrderItemCustomization>` property named `SelectedCustomizations`.
* **Factory Method**: A static `Create(...)` method will be responsible for instantiation and validation.
  * It will calculate the `LineItemTotal` based on base price, quantity, and the sum of customization price adjustments.
  * It will validate that `Quantity` is positive and that snapshot data is not null.
* **Constructor**: The constructor will be `private` to enforce creation via the factory method. A `protected` parameterless constructor will be included for EF Core.

### `PaymentTransaction.cs`

* **Inheritance**: `Entity<PaymentTransactionId>`.
* **Properties**:
  * `Type` (PaymentTransactionType)
  * `Amount` (Money)
  * `Status` (PaymentStatus)
  * `Timestamp` (DateTime)
  * `PaymentGatewayReferenceID` (string, optional)
* **Factory Method**: A static `Create(...)` method will validate that `Amount` is positive.
* **State Transitions**: Public methods like `MarkAsSucceeded()` and `MarkAsFailed()` will update the `Status` property.
* **Constructor**: `private` and `protected` parameterless constructors.

---

## 5. Aggregate Root (`Order.cs`) Implementation

The `Order` class is the core of the aggregate, responsible for managing all its invariants.

* **Inheritance**: `AggregateRoot<OrderId, Guid>`.
* **Properties**:
  * `OrderNumber` (string, human-readable)
  * `Status` (OrderStatus)
  * `PlacementTimestamp`, `LastUpdateTimestamp` (DateTime)
  * `EstimatedDeliveryTime` (DateTime?)
  * `SpecialInstructions` (string)
  * `DeliveryAddress` (DeliveryAddress VO)
  * `Subtotal`, `DiscountAmount`, `DeliveryFee`, `TipAmount`, `TaxAmount`, `TotalAmount` (Money VOs)
  * `CustomerId` (UserId)
  * `RestaurantId` (RestaurantId)
  * `AppliedCouponIDs` (private `List<CouponId>`, public `IReadOnlyList<CouponId>`)
  * `_orderItems` (private `List<OrderItem>`, public `IReadOnlyList<OrderItem>`)
  * `_paymentTransactions` (private `List<PaymentTransaction>`, public `IReadOnlyList<PaymentTransaction>`)
* **Factory Method**: `public static Result<Order> Create(...)`
  * **Parameters**: `customerId`, `restaurantId`, `deliveryAddress`, `items`, `specialInstructions`, etc.
  * **Validation**:
    * Ensure the list of `orderItems` is not empty.
    * It will perform the critical financial calculations:
            1. Calculate `Subtotal` by summing `LineItemTotal` from all `OrderItem`s.
            2. Calculate `TotalAmount` based on the formula: `Subtotal - DiscountAmount + TaxAmount + DeliveryFee + TipAmount`.
            3. Return `Result.Failure` if `TotalAmount` is negative.
  * **Initialization**:
    * Set initial `Status` to `Placed`.
    * Set `PlacementTimestamp` and `LastUpdateTimestamp`.
    * Generate a human-readable `OrderNumber`.
  * **Domain Event**: Raise an `OrderCreated` domain event.
* **Business Logic Methods (State Transitions)**:
  * `public Result Accept(DateTime estimatedDeliveryTime)`: Changes `Status` from `Placed` to `Accepted`. Returns `Failure` if the status is not `Placed`. Sets `EstimatedDeliveryTime`. Raises `OrderAccepted`.
  * `public Result Reject(string reason)`: Changes `Status` from `Placed` to `Rejected`. Returns `Failure` if the status is not `Placed`. Raises `OrderRejected`.
  * `public Result Cancel(UserId cancelledBy)`: Changes `Status` to `Cancelled`. Can only be cancelled if `Status` is `Placed` or `Accepted`. Returns `Failure` otherwise. Raises `OrderCancelled`.
  * Other methods for the fulfillment lifecycle: `MarkAsPreparing()`, `MarkAsReadyForDelivery()`, `MarkAsDelivered()`. Each will validate the current status before transitioning.
* **Payment Methods**:
  * `public Result AddPaymentAttempt(PaymentTransaction payment)`: Adds a new payment transaction to the `_paymentTransactions` list.
  * `public Result MarkAsPaid()`: This method will be called when a payment succeeds. It could potentially trigger an `OrderPaid` event.
* **Constructors**: A `private` constructor for the factory and a `protected` parameterless one for EF Core.

---

## 6. Domain Errors

A static class `Domain.OrderAggregate.Errors.Order` will be created to hold all business rule violation errors.

* `OrderItemRequired`: "An order must have at least one item."
* `InvalidOrderStatusForAccept`: "Order cannot be accepted because it is not in 'Placed' status."
* `InvalidOrderStatusForCancel`: "Order cannot be cancelled at its current stage."
* `NegativeTotalAmount`: "The total amount for an order cannot be negative."

---

## 7. Domain Events

Events will be defined as `record` types implementing `IDomainEvent`.

* `OrderCreated(OrderId orderId, UserId customerId, RestaurantId restaurantId, Money totalAmount)`
* `OrderAccepted(OrderId orderId)`
* `OrderRejected(OrderId orderId)`
* `OrderCancelled(OrderId orderId)`
* `OrderPaid(OrderId orderId, PaymentTransactionId paymentTransactionId)`

This plan provides a comprehensive blueprint for implementing the `Order` aggregate in a way that is consistent with the existing domain architecture and DDD principles.
