# RestaurantAccount Aggregate

## Aggregate Documentation: `RestaurantAccount`

* **Version:** 1.0
* **Last Updated:** 2024-12-13
* **Source File:** `src/Domain/RestaurantAccountAggregate/RestaurantAccount.cs`

### 1. Overview

**Description:**
A lean aggregate that manages a restaurant's current financial balance and payout settings. It is designed to be small and highly performant for frequent financial operations, decoupling it from the full transaction history. The aggregate directly modifies the current balance through behavior-driven methods and raises events for audit trail creation.

**Core Responsibilities:**

* Manages the lifecycle of restaurant financial accounts
* Acts as the transactional boundary for all financial balance operations
* Enforces business rules for positive revenue amounts and negative fees/refunds
* Enforces business rules for sufficient balance during payout settlements

### 2. Structure

* **Aggregate Root:** `RestaurantAccount`
* **Key Value Objects:**
  * `RestaurantAccountId`: Strongly-typed identifier for the aggregate
  * `PayoutMethodDetails`: Stores tokenized bank account information for payouts
  * `Money`: Represents monetary amounts with currency information

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create a `RestaurantAccount` is through its static factory method.

```csharp
public static Result<RestaurantAccount> Create(RestaurantId restaurantId)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantId` | `RestaurantId` | The restaurant this account belongs to |

**Validation Rules & Potential Errors:**

* `restaurantId` must be valid (enforced by type safety)
* Account is created with zero balance and no payout method

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result RecordRevenue(Money amount, OrderId orderId)` | Records positive revenue from an order | Amount must be positive | `RestaurantAccountErrors.OrderRevenueMustBePositive` |
| `Result RecordPlatformFee(Money feeAmount, OrderId orderId)` | Records negative platform fee for an order | Amount must be negative | `RestaurantAccountErrors.PlatformFeeMustBeNegative` |
| `Result RecordRefundDeduction(Money refundAmount, OrderId orderId)` | Records negative refund deduction for an order | Amount must be negative | `RestaurantAccountErrors.RefundDeductionMustBeNegative` |
| `Result SettlePayout(Money payoutAmount)` | Settles a payout to the restaurant | Amount must be positive and ≤ current balance | `RestaurantAccountErrors.PayoutAmountMustBePositive`, `RestaurantAccountErrors.InsufficientBalance` |
| `Result MakeManualAdjustment(Money adjustmentAmount, string reason, Guid adminId)` | Makes a manual balance adjustment by admin | Reason must be provided | `RestaurantAccountErrors.ManualAdjustmentReasonRequired` |
| `Result UpdatePayoutMethod(PayoutMethodDetails payoutMethodDetails)` | Updates the payout method information | None - always succeeds with valid input | None |
| `Result MarkAsDeleted()` | Marks the account as deleted | None - always succeeds | None |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `RestaurantAccountId` | The unique identifier of the aggregate |
| `RestaurantId` | `RestaurantId` | The restaurant this account belongs to |
| `CurrentBalance` | `Money` | The current financial balance (stateful, not calculated) |
| `PayoutMethodDetails` | `PayoutMethodDetails?` | The tokenized payout method information (nullable) |

#### 4.2. Public Query Methods

This aggregate does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `RestaurantAccountCreated` | During the `Create` factory method | Signals that a new restaurant account has been successfully created |
| `RevenueRecorded` | After a successful call to `RecordRevenue` | Signals that revenue from an order has been recorded |
| `PlatformFeeRecorded` | After a successful call to `RecordPlatformFee` | Signals that a platform fee has been deducted |
| `RefundDeducted` | After a successful call to `RecordRefundDeduction` | Signals that a refund has been deducted from the account |
| `PayoutSettled` | After a successful call to `SettlePayout` | Signals that a payout has been settled to the restaurant |
| `ManualAdjustmentMade` | After a successful call to `MakeManualAdjustment` | Signals that a manual adjustment has been made by an admin |
| `PayoutMethodUpdated` | After a successful call to `UpdatePayoutMethod` | Signals that the payout method has been updated |
| `RestaurantAccountDeleted` | After a successful call to `MarkAsDeleted` | Signals that the account has been marked for deletion |
