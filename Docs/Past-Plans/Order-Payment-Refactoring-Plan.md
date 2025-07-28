
# Order Aggregate Payment Refactoring Plan

This document outlines the detailed steps required to refactor the `Order` aggregate's payment processing logic. The goal is to align the implementation with the principles of Domain-Driven Design (DDD), improve encapsulation, and create a more robust and maintainable system.

## 1. `OrderStatus` Enum (`src/Domain/OrderAggregate/Enums/OrderStatus.cs`)

### Changes:
- **Rename `PendingPayment` to `AwaitingPayment`:** This new name more accurately reflects that the order is created but not yet actionable until payment is confirmed.
- **Remove `PaymentFailed`:** The failure of a payment is a status that belongs to the `PaymentTransaction` entity, not the order itself. An order with a failed payment should be considered `Cancelled`.

### New `OrderStatus.cs`:
```csharp
public enum OrderStatus
{
    /// <summary>
    /// The order has been created but is awaiting successful payment 
    /// before being sent to the restaurant. It is not yet actionable.
    /// </summary>
    AwaitingPayment, // Renamed from PendingPayment
    
    /// <summary>
    /// Order has been successfully placed (payment confirmed or COD) 
    /// and is ready for restaurant review.
    /// </summary>
    Placed,
    
    Accepted,
    Preparing,
    ReadyForDelivery,
    Delivered,
    Cancelled,
    Rejected
    
    // REMOVED: PaymentFailed
}
```

## 2. `PaymentTransaction` Entity (`src/Domain/OrderAggregate/Entities/PaymentTransaction.cs`)

### Changes:
- No significant changes are needed to the properties of this entity. The existing design with `PaymentGatewayReferenceId` is correct. The key is to ensure it is used as the single source of truth for payment-gateway-specific identifiers.

## 3. `Order` Aggregate Root (`src/Domain/OrderAggregate/Order.cs`)

### Properties to be Removed:
- **`public string? PaymentIntentId { get; private set; }`**: This property will be removed from the `Order` aggregate root. The responsibility for holding the payment gateway's reference ID will be delegated entirely to the `PaymentTransaction` entity.

### `Create` Method Refactoring:
- The static `Create` factory method will be significantly refactored.
- **New Parameters:**
    - `PaymentMethodType paymentMethodType`: This is now required to determine how to handle the initial payment transaction.
    - `string? paymentGatewayReferenceId = null`: This will hold the Stripe Payment Intent ID (or any other gateway's reference) and will be passed directly to the `PaymentTransaction`.
- **Removed Parameters:**
    - The separate `Create` overloads will be consolidated.
- **New Logic:**
    - The method will be responsible for creating the *initial* `PaymentTransaction`.
    - **For `CashOnDelivery`:** It will create a `PaymentTransaction`, immediately mark it as `Succeeded`, and set the `OrderStatus` to `Placed`.
    - **For Online Payments:** It will create a `PaymentTransaction` with a `Pending` status and the provided `paymentGatewayReferenceId`. The `OrderStatus` will be `AwaitingPayment`.
    - It will validate that `paymentGatewayReferenceId` is provided for online payments.

### Methods to be Removed:
- **`public Result ConfirmPayment(DateTime? timestamp = null)`**: This method will be replaced by `RecordPaymentSuccess`.
- **`public Result MarkAsPaymentFailed(DateTime? timestamp = null)`**: This method will be replaced by `RecordPaymentFailure`.

### New Methods to be Added:
- **`public Result RecordPaymentSuccess(string paymentGatewayReferenceId, DateTime? timestamp = null)`**:
    - This method will be called when a payment is successfully processed (e.g., via a webhook).
    - It will find the corresponding `PaymentTransaction` using the `paymentGatewayReferenceId`.
    - It will call `MarkAsSucceeded()` on the transaction.
    - It will change the `Order.Status` to `Placed`.
    - It will raise an `OrderPaymentSucceeded` domain event.
- **`public Result RecordPaymentFailure(string paymentGatewayReferenceId, DateTime? timestamp = null)`**:
    - This method will be called when a payment fails.
    - It will find the corresponding `PaymentTransaction`.
    - It will call `MarkAsFailed()` on the transaction.
    - It will change the `Order.Status` to `Cancelled`.
    - It will raise an `OrderPaymentFailed` domain event.

## 4. Domain Events (`src/Domain/OrderAggregate/Events/`)

### Changes:
- **`OrderPaymentSucceeded.cs`**: This event is already correctly named and can be reused.
- **`OrderPaymentFailed.cs`**: This event is also correctly named and can be reused.
- **No new events are required.** The existing events are sufficient.

## 5. `OrderErrors.cs` (`src/Domain/OrderAggregate/Errors/OrderErrors.cs`)

### Errors to be Added:
- **`PaymentGatewayReferenceIdRequired`**: To be used in the `Order.Create` method when an online payment is being processed without a gateway reference ID.
- **`PaymentTransactionNotFound`**: To be used in `RecordPaymentSuccess` and `RecordPaymentFailure` if a transaction with the given `paymentGatewayReferenceId` cannot be found.

### Errors to be Modified/Reviewed:
- **`InvalidStatusForPaymentConfirmation`**: The error message should be updated to reflect the new state logic (e.g., "Payment can only be processed for orders in 'AwaitingPayment' status.").
- **`PaymentIntentIdRequired`**: This can be removed or repurposed into `PaymentGatewayReferenceIdRequired`.

## Summary of Unused Files/Code:
- The concept of `PaymentIntentId` on the `Order` aggregate will be completely removed.
- The `ConfirmPayment` and `MarkAsPaymentFailed` methods on the `Order` aggregate will be removed.
- The `OrderStatus.PaymentFailed` enum member will be removed.

This refactoring will result in a cleaner, more decoupled design where the `Order` aggregate is not directly concerned with the specifics of the payment gateway, leading to a more robust and maintainable system.














---

## 6. Unit Test Refactoring Plan (`tests/Domain.UnitTests/OrderAggregate/`)

This section outlines the plan to update the unit tests for the `Order` aggregate to reflect the recent refactoring. The goal is to ensure continued high test coverage and to validate the new payment-related logic.

### A. `OrderCreationTests.cs`

- **Tests to be Updated:**
    - `Create_WithValidInputs_ShouldSucceedAndInitializeOrderCorrectly`: This test needs to be updated to use the new `Order.Create` signature. It should test both `CashOnDelivery` and online payment scenarios.
    - `Create_WithMinimalInputs_ShouldSucceedWithDefaults`: Similar to the above, this test needs to be updated to use the new `Order.Create` signature.
    - `Create_WithCoupon_ShouldSucceedAndStoreCoupon`: Update to use the new `Order.Create` signature.
- **Tests to be Removed:**
    - `Create_WithSpecifiedInitialStatus_ShouldSetCorrectStatus`: This test is now redundant as the `Create` method determines the initial status internally based on the `PaymentMethodType`.
    - `Create_WithPaymentIntentId_ShouldStorePaymentIntentId`: This is no longer relevant as the `PaymentIntentId` is not stored on the `Order` aggregate directly.
    - `Create_WithPendingPaymentStatus_ShouldNotRequirePaymentTransactions`: The new `Create` method handles this logic internally, so a separate test is not required.
- **Tests to be Added:**
    - `Create_WithOnlinePayment_ShouldCreatePendingTransactionAndSetStatusToAwaitingPayment`: This test will verify that for online payments, a `PaymentTransaction` is created with a `Pending` status and the `OrderStatus` is set to `AwaitingPayment`.
    - `Create_WithCashOnDelivery_ShouldCreateSucceededTransactionAndSetStatusToPlaced`: This test will verify that for `CashOnDelivery` payments, a `PaymentTransaction` is created with a `Succeeded` status and the `OrderStatus` is set to `Placed`.
    - `Create_WithOnlinePayment_AndMissingGatewayReferenceId_ShouldFail`: This test will ensure that an online payment order cannot be created without a `paymentGatewayReferenceId`.

### B. `OrderPaymentFlowTests.cs`

- **Tests to be Updated:**
    - `PendingPaymentOrder_ConfirmPayment_ShouldTransitionToPlaced`: This test will be renamed to `AwaitingPaymentOrder_RecordPaymentSuccess_ShouldTransitionToPlaced` and updated to use the new `RecordPaymentSuccess` method.
    - `PendingPaymentOrder_MarkAsPaymentFailed_ShouldTransitionToPaymentFailed`: This test will be renamed to `AwaitingPaymentOrder_RecordPaymentFailure_ShouldTransitionToCancelled` and updated to use the new `RecordPaymentFailure` method. The expected final status is now `Cancelled`.
    - `CompletePaymentFlow_FromPendingToPlacedToDelivered`: This test will be updated to use the new methods and enums (`AwaitingPayment`, `RecordPaymentSuccess`).
    - `CompletePaymentFlow_FromPendingToFailedIsTerminal`: This test will be updated to reflect that a failed payment now results in a `Cancelled` status, which is also a terminal state.
- **Tests to be Removed:**
    - `CreateOrder_WithPendingPaymentStatus_ShouldSucceed`: This is covered by the new tests in `OrderCreationTests.cs`.
    - `Create_WithPaymentIntentId_ShouldStoreId`: No longer relevant.
    - `Create_WithPendingPaymentStatus_RequiresPaymentIntentId`: This is covered by the new `Create_WithOnlinePayment_AndMissingGatewayReferenceId_ShouldFail` test.
    - `PaymentFailedOrder_CannotTransitionToOtherStates`: This test will be replaced with a new test to verify that a `Cancelled` order cannot be transitioned to other states.
- **Tests to be Added:**
    - `CancelledOrder_CannotTransitionToOtherStates`: This test will verify that an order in the `Cancelled` state cannot be transitioned to any other state.
    - `RecordPaymentSuccess_WithInvalidGatewayReferenceId_ShouldFail`: This test will ensure that `RecordPaymentSuccess` fails if the provided `paymentGatewayReferenceId` does not match any transaction.
    - `RecordPaymentFailure_WithInvalidGatewayReferenceId_ShouldFail`: This test will ensure that `RecordPaymentFailure` fails if the provided `paymentGatewayReferenceId` does not match any transaction.

### C. `OrderPaymentTests.cs`

- **This entire test file can be removed.** The tests in this file are now redundant and their concerns are better covered by the updated tests in `OrderCreationTests.cs` and `OrderPaymentFlowTests.cs`.
Th
### D. Test Helpers (`OrderTestHelpers.cs`)

- **Update `CreatePendingPaymentOrder`:** This helper method will need to be updated to use the new `Order.Create` method with an online payment type to create an order in the `AwaitingPayment` state.
- **Remove `CreatePaymentFailedOrder`:** This helper is no longer needed as the `PaymentFailed` status has been removed.
- **Update `CreateValidOrder`:** Ensure this helper creates an order with a `Placed` status, which is now the default for `CashOnDelivery` or a successfully paid online order.

By following this plan, we will ensure that the unit tests for the `Order` aggregate are up-to-date, comprehensive, and accurately reflect the new payment processing logic.