# AccountTransaction Entity

## Entity Documentation: `AccountTransaction`

* **Version:** 1.0
* **Last Updated:** 2024-12-13
* **Source File:** `src/Domain/AccountTransactionEntity/AccountTransaction.cs`

### 1. Overview

**Description:**
An immutable, historical record of a single financial event that has occurred on a RestaurantAccount. It serves as the official audit log and is created in response to domain events from the RestaurantAccount aggregate. This entity is not part of the RestaurantAccount aggregate's boundary and is managed by event handlers in the Application Layer.

**Core Responsibilities:**

* Records immutable financial transaction history
* Acts as an audit trail for all RestaurantAccount balance changes
* Enforces business rules for transaction amount sign validation based on type
* Links transactions to related orders for comprehensive auditing

### 2. Structure

* **Entity Root:** `AccountTransaction`
* **Key Value Objects:**
  * `AccountTransactionId`: Strongly-typed identifier for the transaction
  * `TransactionType`: Enumeration of valid transaction types (OrderRevenue, PlatformFee, etc.)
  * `Money`: Represents monetary amounts with currency information

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create an `AccountTransaction` is through its static factory method.

```csharp
public static Result<AccountTransaction> Create(
    RestaurantAccountId restaurantAccountId,
    TransactionType type,
    Money amount,
    OrderId? relatedOrderId = null,
    string? notes = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantAccountId` | `RestaurantAccountId` | The restaurant account this transaction belongs to |
| `type` | `TransactionType` | The type of transaction (OrderRevenue, PlatformFee, etc.) |
| `amount` | `Money` | The transaction amount (positive for credits, negative for debits) |
| `relatedOrderId` | `OrderId?` | Optional order ID linking transaction to specific order |
| `notes` | `string?` | Optional notes (used for manual adjustments, etc.) |

**Validation Rules & Potential Errors:**

* `OrderRevenue` amounts must be positive. (Returns `RestaurantAccountErrors.OrderRevenueMustBePositive`)
* `PlatformFee` amounts must be negative. (Returns `RestaurantAccountErrors.PlatformFeeMustBeNegative`)
* `RefundDeduction` amounts must be negative. (Returns `RestaurantAccountErrors.RefundDeductionMustBeNegative`)
* `PayoutSettlement` and `ManualAdjustment` types have no amount sign restrictions
* Timestamp is automatically set to `DateTime.UtcNow`

#### 3.2. State Transitions & Commands (Public Methods)

This entity is immutable once created. There are no public methods that modify its state.

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `AccountTransactionId` | The unique identifier of the transaction |
| `RestaurantAccountId` | `RestaurantAccountId` | The restaurant account this transaction belongs to |
| `Type` | `TransactionType` | The type of financial transaction |
| `Amount` | `Money` | The transaction amount (positive for credits, negative for debits) |
| `Timestamp` | `DateTime` | When the transaction was recorded (UTC) |
| `RelatedOrderId` | `OrderId?` | Optional order ID for audit trail linking |
| `Notes` | `string?` | Optional notes for additional context |

#### 4.2. Public Query Methods

This entity does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

This entity does not raise domain events. It is created in response to events from other aggregates and serves as a passive audit record.

### 6. Transaction Types

The `TransactionType` enum defines the following valid transaction types:

| Transaction Type | Description | Amount Sign | Related Order Required |
| :--- | :--- | :--- | :--- |
| `OrderRevenue` | Revenue from customer orders | Positive (credit) | Yes |
| `PlatformFee` | Platform fees deducted by the system | Negative (debit) | Yes |
| `RefundDeduction` | Refunds deducted from restaurant balance | Negative (debit) | Yes |
| `PayoutSettlement` | Payouts settled to restaurant | Negative (debit) | No |
| `ManualAdjustment` | Manual adjustments by administrators | Any sign | No |

### 7. Creation Context

This entity is not created directly by application services but rather by event handlers that subscribe to the following RestaurantAccount domain events:

* `RevenueRecorded` → Creates `OrderRevenue` transaction
* `PlatformFeeRecorded` → Creates `PlatformFee` transaction  
* `RefundDeducted` → Creates `RefundDeduction` transaction
* `PayoutSettled` → Creates `PayoutSettlement` transaction
* `ManualAdjustmentMade` → Creates `ManualAdjustment` transaction

This decoupling ensures that the RestaurantAccount aggregate remains focused on balance management while the transaction history is maintained separately for audit purposes.
