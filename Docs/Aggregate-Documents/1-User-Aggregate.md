# Aggregate Documentation: `User`

* **Version:** 1.0
* **Last Updated:** 2025-07-05
* **Source File:** `e:\source\repos\CA\YummyZoom\src\Domain\UserAggregate\User.cs`

### 1. Overview

**Description:**
*Manages user information, including profile, addresses, and payment methods. It serves as the central point for all user-related operations and business rule enforcement.*

**Core Responsibilities:**

* Manages the lifecycle of a user account.
* Acts as the transactional boundary for all user-related operations.
* Enforces business rules for user profile information.
* Manages user addresses and payment methods.

### 2. Structure

* **Aggregate Root:** `User`
* **Key Child Entities:**
  * `Address`: Represents a user's shipping or billing address.
  * `PaymentMethod`: Represents a user's payment information.
* **Key Value Objects:**
  * `UserId`: The unique identifier for the `User` aggregate.
  * `AddressId`: The unique identifier for the `Address` entity.
  * `PaymentMethodId`: The unique identifier for the `PaymentMethod` entity.

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create a `User` is through its static factory method.

```csharp
public static Result<User> Create(
  string name,
  string email,
  string? phoneNumber = null
)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `name` | `string` | The user's full name. |
| `email` | `string` | The user's unique email address. |
| `phoneNumber` | `string?` | The user's optional phone number. |

**Validation Rules & Potential Errors:**

* None directly in the factory, but assumes `name` and `email` are validated by their respective value objects or types if they exist.

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result AddAddress(Address address)` | Adds a new address to the user's list. | None. | None. |
| `Result RemoveAddress(AddressId addressId)` | Removes an address from the user's list. | Checks if the address exists. | `UserErrors.AddressNotFound` |
| `Result AddPaymentMethod(PaymentMethod paymentMethod)` | Adds a new payment method. | If the new method is default, it ensures no other method is default. | None. |
| `Result RemovePaymentMethod(PaymentMethodId paymentMethodId)` | Removes a payment method. | Checks if the payment method exists. | `UserErrors.PaymentMethodNotFound` |
| `Result SetDefaultPaymentMethod(PaymentMethodId paymentMethodId)` | Sets a payment method as the default. | Checks if the payment method exists. | `UserErrors.PaymentMethodNotFound` |
| `Result UpdateProfile(string name, string? phoneNumber)` | Updates the user's name and phone number. | None. | None. |
| `Result UpdateEmail(string email)` | Updates the user's email address. | None. | None. |
| `Result Activate()` | Activates the user's account. | None. | None. |
| `Result Deactivate()` | Deactivates the user's account. | None. | None. |
| `Result MarkAsDeleted(bool forceDelete = false)` | Marks the user as deleted for soft deletion. | None. | None. |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `UserId` | The unique identifier of the aggregate. |
| `Name` | `string` | The user's name. |
| `Email` | `string` | The user's email address. |
| `PhoneNumber` | `string?` | The user's phone number. |
| `IsActive` | `bool` | The current status of the user's account. |
| `Addresses` | `IReadOnlyList<Address>` | A read-only list of the user's addresses. |
| `PaymentMethods` | `IReadOnlyList<PaymentMethod>` | A read-only list of the user's payment methods. |

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `UserCreated` | During the `Create` factory method. | Signals that a new user has been successfully created. |
| `UserAddressAdded` | After a successful call to `AddAddress`. | Signals that a new address was added to the user. |
| `UserAddressRemoved` | After a successful call to `RemoveAddress`. | Signals that an address was removed from the user. |
| `UserPaymentMethodAdded` | After a successful call to `AddPaymentMethod`. | Signals that a new payment method was added. |
| `UserPaymentMethodRemoved` | After a successful call to `RemovePaymentMethod`. | Signals that a payment method was removed. |
| `UserDefaultPaymentMethodChanged` | After a successful call to `SetDefaultPaymentMethod`. | Signals that the user's default payment method has changed. |
| `UserProfileUpdated` | After a successful call to `UpdateProfile`. | Signals that the user's profile has been updated. |
| `UserEmailChanged` | After a successful call to `UpdateEmail`. | Signals that the user's email has changed. |
| `UserActivated` | After a successful call to `Activate`. | Signals that the user's account has been activated. |
| `UserDeactivated` | After a successful call to `Deactivate`. | Signals that the user's account has been deactivated. |
| `UserDeleted` | After a successful call to `MarkAsDeleted`. | Signals that the user has been marked for deletion. |
