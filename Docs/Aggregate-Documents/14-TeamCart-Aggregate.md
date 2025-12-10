# Aggregate Documentation: `TeamCart`

* **Version:** 1.0
* **Last Updated:** 2025-07-18
* **Source File:** `src/Domain/TeamCartAggregate/TeamCart.cs`

## 1. Overview

**Description:**
The `TeamCart` aggregate represents a collaborative shopping cart where multiple users can add items before converting to a final Order. It manages the entire lifecycle of team-based ordering, from creation and member invitation through item selection, payment collection, and final conversion to an Order. The aggregate enables mixed payment methods (online and cash on delivery) and provides a seamless collaborative ordering experience.

**Core Responsibilities:**

* Manages the complete lifecycle of collaborative food ordering from creation through conversion to an Order.
* Acts as the transactional boundary for member management, item addition, and payment collection.
* Enforces business rules for member roles, payment validation, and cart status transitions.
* Provides flexible payment options with support for mixed payment methods (online and cash on delivery).
* Enables financial management including tips and coupon application.

## 2. Structure

* **Aggregate Root:** `TeamCart`
* **Key Child Entities:**
  * `TeamCartMember`: Represents a participant in the team cart with a specific role.
  * `TeamCartItem`: Represents a menu item added to the cart with customizations.
  * `MemberPayment`: Tracks payment commitments and completed transactions by members.
* **Key Value Objects:**
  * `TeamCartId`: Unique identifier for the team cart.
  * `ShareableLinkToken`: Token used for inviting others to join the team cart.
  * `TeamCartItemCustomization`: Represents customization choices for menu items.
* **Key Enums:**
  * `TeamCartStatus`: Cart lifecycle states (Open, Locked, Finalized, ReadyToConfirm, Converted, Expired).
  * `MemberRole`: Role of a member in the team cart (Host, Guest).
  * `PaymentMethod`: Payment method types (Online, CashOnDelivery).
  * `PaymentStatus`: Status of a payment (Pending, CommittedToCOD, PaidOnline, Failed).

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `TeamCart` is through its static factory method.

```csharp
public static Result<TeamCart> Create(
    UserId hostUserId,
    RestaurantId restaurantId,
    string hostName,
    DateTime? deadline = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `hostUserId` | `UserId` | The ID of the user creating and hosting the team cart. |
| `restaurantId` | `RestaurantId` | The ID of the restaurant for this team cart. |
| `hostName` | `string` | The display name of the host user. |
| `deadline` | `DateTime?` | Optional deadline for ordering (defaults to 24 hours from creation). |

**Validation Rules & Potential Errors:**

* `hostName` cannot be null or empty. (Returns `TeamCartErrors.HostNameRequired`)
* `deadline` must be in the future. (Returns `TeamCartErrors.DeadlineInPast`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result AddMember(UserId userId, string name, MemberRole role = MemberRole.Guest)` | Adds a new member to the team cart. | Checks if cart is open and member doesn't already exist. | `TeamCartErrors.MemberNameRequired`, `TeamCartErrors.CannotAddMembersToClosedCart`, `TeamCartErrors.MemberAlreadyExists` |
| `Result SetDeadline(UserId requestingUserId, DateTime deadline)` | Sets or updates the deadline for the team cart. | Validates that requestor is host and deadline is in future. | `TeamCartErrors.OnlyHostCanSetDeadline`, `TeamCartErrors.CannotModifyClosedCart`, `TeamCartErrors.DeadlineInPast` |
| `Result AddItem(UserId userId, MenuItemId menuItemId, MenuCategoryId menuCategoryId, string itemName, Money basePrice, int quantity, List<TeamCartItemCustomization>? customizations = null)` | Adds an item to the team cart with optional customizations. | Checks if cart is open and user is a member. | `TeamCartErrors.CannotAddItemsToClosedCart`, `TeamCartErrors.UserNotMember`, `TeamCartErrors.InvalidQuantity` |
| `Result LockForPayment(UserId requestingUserId)` | Locks the team cart, preventing further item modifications. Host can now adjust tips/coupons. | Validates that requestor is host and cart has items. | `TeamCartErrors.OnlyHostCanLockCart`, `TeamCartErrors.CannotLockCartInCurrentStatus`, `TeamCartErrors.CannotLockEmptyCart` |
| `Result FinalizePricing()` | Finalizes pricing after tip/coupon adjustments, enabling member payments. Two-phase lock pattern prevents race conditions. | Validates cart is in Locked status. | `TeamCartErrors.CannotFinalizePricingInCurrentStatus` |
| `Result CommitToCashOnDelivery(UserId userId, Money amount)` | Records a member's firm commitment to pay with Cash on Delivery. | Validates user is member, cart is finalized, and amount matches their items. | `TeamCartErrors.CanOnlyPayOnFinalizedCart`, `TeamCartErrors.UserNotMember`, `TeamCartErrors.InvalidPaymentAmount` |
| `Result RecordSuccessfulOnlinePayment(UserId userId, Money amount, string transactionId)` | Records a successful online payment after confirmation by payment gateway. | Validates user is member, cart is finalized, amount matches their items, and transaction ID is valid. | `TeamCartErrors.CanOnlyPayOnFinalizedCart`, `TeamCartErrors.UserNotMember`, `TeamCartErrors.InvalidPaymentAmount`, `TeamCartErrors.InvalidTransactionId` |
| `Result ApplyTip(UserId requestingUserId, Money tipAmount)` | Adds or updates the tip amount for the team cart. | Validates requestor is host and tip is non-negative. | `TeamCartErrors.OnlyHostCanModifyFinancials`, `TeamCartErrors.CanOnlyApplyFinancialsToLockedCart`, `TeamCartErrors.InvalidTip` |
| `Result ApplyCoupon(UserId requestingUserId, CouponId couponId)` | Applies a coupon to the team cart by storing its ID. | Validates requestor is host and cart is locked. | `TeamCartErrors.OnlyHostCanModifyFinancials`, `TeamCartErrors.CanOnlyApplyFinancialsToLockedCart`, `TeamCartErrors.CouponAlreadyApplied` |
| `Result RemoveCoupon(UserId requestingUserId)` | Removes the currently applied coupon. | Validates requestor is host. | `TeamCartErrors.OnlyHostCanModifyFinancials` |
| `Result MarkAsExpired()` | Marks the team cart as expired. | None | None |
| `Result MarkAsConverted()` | Marks the TeamCart as converted after successful Order creation. | Validates cart is in ReadyToConfirm status. | `TeamCartErrors.InvalidStatusForConversion` |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `TeamCartId` | The unique identifier of the team cart. |
| `RestaurantId` | `RestaurantId` | The ID of the restaurant for this team cart. |
| `HostUserId` | `UserId` | The ID of the user who created and hosts this team cart. |
| `Status` | `TeamCartStatus` | The current status of the team cart. |
| `ShareToken` | `ShareableLinkToken` | The shareable token used for joining this team cart. |
| `Deadline` | `DateTime?` | The optional deadline set by the host for ordering. |
| `CreatedAt` | `DateTime` | The timestamp when the team cart was created. |
| `ExpiresAt` | `DateTime` | The timestamp when the team cart will automatically expire. |
| `Members` | `IReadOnlyList<TeamCartMember>` | A read-only list of members in this team cart. |
| `Items` | `IReadOnlyList<TeamCartItem>` | A read-only list of items in this team cart. |
| `MemberPayments` | `IReadOnlyList<MemberPayment>` | A read-only list of member payments in this team cart. |
| `TipAmount` | `Money` | The tip amount for the order, set by the Host. |
| `AppliedCouponId` | `CouponId?` | The ID of the coupon applied to the team cart. |

### 4.2. Public Query Methods

These methods provide information about the aggregate's state without changing it.

| Method Signature | Description |
| :--- | :--- |
| `bool IsExpired()` | Returns `true` if the team cart has expired based on its expiration time or deadline. |
| `Result ValidateJoinToken(string token)` | Validates if a user can join the team cart using the provided token. |

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `TeamCartCreated` | During the `Create` factory method. | Signals that a new team cart has been created with initial details. |
| `MemberJoined` | After a successful call to `AddMember` (for guests only). | Signals that a new member has joined the team cart. |
| `ItemAddedToTeamCart` | After a successful call to `AddItem`. | Signals that a new item has been added to the team cart. |
| `TeamCartLockedForPayment` | After a successful call to `LockForPayment`. | Signals that the team cart has been locked for payment. |
| `MemberCommittedToPayment` | After a successful call to `CommitToCashOnDelivery`. | Signals that a member has committed to pay with cash on delivery. |
| `OnlinePaymentSucceeded` | After a successful call to `RecordSuccessfulOnlinePayment`. | Signals that a member has successfully completed an online payment. |
| `TeamCartReadyForConfirmation` | When all members have committed to payment. | Signals that the team cart is ready to be converted to an order, including total and cash amounts. |
| `TeamCartExpired` | After a successful call to `MarkAsExpired`. | Signals that the team cart has expired. |
| `TeamCartConverted` | After a successful conversion to an Order. | Signals that the team cart has been successfully converted to an Order. |

## 6. Key Child Entities

### 6.1. TeamCartMember

Represents a participant in the team cart.

**Properties:**

* `Id` (TeamCartMemberId): Unique identifier
* `UserId` (UserId): ID of the user associated with this member
* `Name` (string): Display name of the member
* `Role` (MemberRole): Role of the member in the team cart (Host or Guest)

**Factory Method:**

```csharp
public static Result<TeamCartMember> Create(UserId userId, string name, MemberRole role = MemberRole.Guest)
```

**Business Rules:**

* Member name cannot be empty
* User ID is required
* Each member has a specific role (Host or Guest)

### 6.2. TeamCartItem

Represents a menu item added to the team cart.

**Properties:**

* `Id` (TeamCartItemId): Unique identifier
* `AddedByUserId` (UserId): ID of the user who added this item
* `Snapshot_MenuItemId` (MenuItemId): ID of the menu item
* `Snapshot_MenuCategoryId` (MenuCategoryId): ID of the menu category
* `Snapshot_ItemName` (string): Name of the menu item
* `Snapshot_BasePriceAtOrder` (Money): Base price of the menu item
* `Quantity` (int): Quantity of the item
* `LineItemTotal` (Money): Total price for this line item
* `SelectedCustomizations` (`IReadOnlyList<TeamCartItemCustomization>`): Customizations for this item

### 6.3. MemberPayment

Tracks payment commitments and completed transactions by members.

**Properties:**

* `Id` (MemberPaymentId): Unique identifier
* `UserId` (UserId): ID of the user who made this payment
* `Amount` (Money): Amount of money for this payment
* `Method` (PaymentMethod): Payment method (Online or CashOnDelivery)
* `Status` (PaymentStatus): Current status of this payment
* `OnlineTransactionId` (string?): Online transaction ID if this was an online payment
* `CreatedAt` (DateTime): Timestamp when this payment was created
* `UpdatedAt` (DateTime): Timestamp when this payment was last updated

**Factory Method:**

```csharp
public static Result<MemberPayment> Create(UserId userId, Money amount, PaymentMethod method)
```

**Public Methods:**

* `Result MarkAsPaidOnline(string transactionId)`: Marks the payment as successfully paid online
* `Result MarkAsFailed()`: Marks the online payment as failed
* `bool IsComplete()`: Checks if this payment is complete
* `bool HasFailed()`: Checks if this payment has failed
* `string GetStatusDisplayName()`: Gets a display name for the payment status

## 7. Business Rules & Invariants

### 7.1. Member Management

* Every team cart must have exactly one Host who cannot be removed
* Members can only be added when the cart is in Open status
* Member names must be provided for display purposes
* A user cannot be added to the team cart more than once

### 7.2. Status Transitions

* Valid transitions are enforced by business logic:
  * Open → Locked (via LockForPayment) - Items frozen, tip/coupon adjustment enabled
  * Locked → Finalized (via FinalizePricing) - Pricing locked, payments enabled
  * Finalized → ReadyToConfirm (automatic when all payments complete)
  * ReadyToConfirm → Converted (via external conversion service)
  * Any status → Expired (via expiration)

* **Two-Phase Lock Pattern:** The Locked → Finalized transition implements a two-phase lock to prevent race conditions where the host adjusts pricing while members are paying. In Locked state, only host can modify tip/coupon. Once Finalized, pricing is immutable and members can pay.

### 7.3. Payment Rules

* Mixed payment methods are allowed within a single cart
* Each member can only have one payment commitment at a time
* Payment amount must match the total of items added by that member
* Payments can only be made when cart is in Finalized status (after host finalizes pricing)
* All online payments must be completed before the cart can transition to ReadyToConfirm
* Host acts as guarantor for all Cash on Delivery payments

### 7.4. Financial Management

* Only the host can modify financial details (tip, coupons)
* Financial details can only be modified in Locked status (before finalization)
* Once pricing is finalized, no more tip/coupon changes are allowed
* Only one coupon can be applied at a time, and it's stored by ID without immediate calculation
* Tip amount cannot be negative

### 7.5. Conversion Rules

* Cart must be in ReadyToConfirm status to be converted to an Order
* All members must have committed to payment before conversion
* Cart must have at least one item to be converted

## 8. Value Objects

### 8.1. TeamCartId

Unique identifier for the team cart.

**Factory Methods:**

* `TeamCartId.CreateUnique()` - Creates a new unique ID
* `TeamCartId.Create(Guid value)` - Creates from an existing GUID

### 8.2. ShareableLinkToken

Token used for inviting others to join the team cart.

**Factory Methods:**

* `ShareableLinkToken.CreateUnique(TimeSpan validFor)` - Creates a new unique token valid for the specified duration

**Properties:**

* `Value` (string): The token value
* `ExpiresAt` (DateTime): When the token expires
* `IsExpired` (bool): Whether the token has expired

### 8.3. TeamCartItemCustomization

Represents customization choices for menu items.

**Factory Method:**

```csharp
public static Result<TeamCartItemCustomization> Create(
    string snapshotCustomizationGroupName,
    string snapshotChoiceName,
    Money snapshotChoicePriceAdjustmentAtOrder)
```

**Properties:**

* `Snapshot_CustomizationGroupName` (string): Name of the customization group
* `Snapshot_ChoiceName` (string): Name of the selected choice
* `Snapshot_ChoicePriceAdjustmentAtOrder` (Money): Price adjustment for this customization

## 9. External Dependencies

The aggregate references other aggregates by ID only:

* `User` entities - For member identification
* `Restaurant` entities - For restaurant context
* `MenuItem` entities - For menu item details
* `MenuCategory` entities - For menu category context
* `Coupon` entities - For discount application

The TeamCart aggregate is designed to be converted to an Order aggregate through a dedicated domain service (`TeamCartConversionService`), which handles the complex mapping between the collaborative cart and the final order.
