# Aggregate Documentation: `Coupon`

* **Version:** 1.0
* **Last Updated:** 2025-01-05
* **Source File:** `src/Domain/CouponAggregate/Coupon.cs`

## 1. Overview

**Description:**
The `Coupon` aggregate manages promotional coupons within a restaurant's promotional system. It encapsulates all business rules related to coupon creation, validation, usage tracking, and lifecycle management. The aggregate ensures that coupon codes are unique within a restaurant, usage limits are enforced, and validity periods are respected.

**Core Responsibilities:**

* Manages the complete lifecycle of promotional coupons from creation to deletion.
* Acts as the transactional boundary for all coupon-related operations and state changes.
* Enforces business rules for coupon validity periods, usage limits, and applicability scope.
* Tracks global usage counts while delegating per-user usage tracking to external services.

## 2. Structure

* **Aggregate Root:** `Coupon`
* **Key Value Objects:**
  * `CouponId`: Unique identifier for the coupon.
  * `CouponValue`: Represents the discount value (percentage, fixed amount, or free item).
  * `AppliesTo`: Defines the scope of items the coupon applies to (whole order, specific items, or categories).
  * `Money`: Represents monetary amounts for fixed discounts and minimum order requirements.

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `Coupon` is through its static factory method.

```csharp
public static Result<Coupon> Create(
    RestaurantId restaurantId,
    string code,
    string description,
    CouponValue value,
    AppliesTo appliesTo,
    DateTime validityStartDate,
    DateTime validityEndDate,
    Money? minOrderAmount = null,
    int? totalUsageLimit = null,
    int? usageLimitPerUser = null,
    bool isEnabled = true)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantId` | `RestaurantId` | The restaurant that owns this coupon. |
| `code` | `string` | Unique code customers enter (normalized to uppercase, max 50 chars). |
| `description` | `string` | Human-readable description of the coupon. |
| `value` | `CouponValue` | The discount value (percentage, fixed amount, or free item). |
| `appliesTo` | `AppliesTo` | Scope definition (whole order, specific items, or categories). |
| `validityStartDate` | `DateTime` | When the coupon becomes valid for use. |
| `validityEndDate` | `DateTime` | When the coupon expires. |
| `minOrderAmount` | `Money?` | Optional minimum order amount requirement. |
| `totalUsageLimit` | `int?` | Optional global usage limit across all users. |
| `usageLimitPerUser` | `int?` | Optional per-user usage limit (enforced externally). |
| `isEnabled` | `bool` | Whether the coupon is initially enabled (default: true). |

**Validation Rules & Potential Errors:**

* `code` cannot be null, empty, or exceed 50 characters. (Returns `CouponErrors.CouponCodeEmpty`, `CouponErrors.CouponCodeTooLong`)
* `description` cannot be null or empty. (Returns `CouponErrors.CouponDescriptionEmpty`)
* `validityEndDate` must be after `validityStartDate`. (Returns `CouponErrors.InvalidValidityPeriod`)
* `totalUsageLimit` must be greater than 0 when specified. (Returns `CouponErrors.InvalidUsageLimit`)
* `usageLimitPerUser` must be greater than 0 when specified. (Returns `CouponErrors.InvalidPerUserLimit`)
* `minOrderAmount` must be greater than 0 when specified. (Returns `CouponErrors.InvalidMinOrderAmount`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result Use(DateTime? usageTime = null)` | Increments usage count when coupon is applied to an order. | Checks enabled status, validity period, and total usage limit. | `CouponErrors.CouponDisabled`, `CouponErrors.CouponNotYetValid`, `CouponErrors.CouponExpired`, `CouponErrors.UsageLimitExceeded` |
| `Result Enable(DateTime? enabledTime = null)` | Enables the coupon for use. | None (idempotent operation). | None |
| `Result Disable(DateTime? disabledTime = null)` | Disables the coupon, preventing further use. | None (idempotent operation). | None |
| `Result UpdateDescription(string description)` | Updates the coupon's description. | Description cannot be null or empty. | `CouponErrors.CouponDescriptionEmpty` |
| `Result SetMinimumOrderAmount(Money? minOrderAmount)` | Sets or updates minimum order amount requirement. | Amount must be greater than 0 when specified. | `CouponErrors.InvalidMinOrderAmount` |
| `Result RemoveMinimumOrderAmount()` | Removes minimum order amount requirement. | None. | None |
| `Result SetTotalUsageLimit(int? totalUsageLimit)` | Sets or updates global usage limit. | Limit must be greater than 0 and not less than current usage. | `CouponErrors.InvalidUsageLimit`, `CouponErrors.UsageCountCannotExceedLimit` |
| `Result RemoveTotalUsageLimit()` | Removes global usage limit (unlimited). | None. | None |
| `Result SetPerUserUsageLimit(int? usageLimitPerUser)` | Sets or updates per-user usage limit. | Limit must be greater than 0 when specified. | `CouponErrors.InvalidPerUserLimit` |
| `Result RemovePerUserUsageLimit()` | Removes per-user usage limit (unlimited per user). | None. | None |
| `Result MarkAsDeleted()` | Marks the coupon as deleted (soft delete). | None. | None |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `CouponId` | The unique identifier of the coupon. |
| `RestaurantId` | `RestaurantId` | The restaurant that owns this coupon. |
| `Code` | `string` | The code customers enter (normalized to uppercase). |
| `Description` | `string` | Human-readable description of the coupon. |
| `Value` | `CouponValue` | The discount value (percentage, fixed amount, or free item). |
| `AppliesTo` | `AppliesTo` | Defines what items/categories the coupon applies to. |
| `MinOrderAmount` | `Money?` | Optional minimum order amount requirement. |
| `ValidityStartDate` | `DateTime` | When the coupon becomes valid for use. |
| `ValidityEndDate` | `DateTime` | When the coupon expires. |
| `TotalUsageLimit` | `int?` | Optional global usage limit across all users. |
| `CurrentTotalUsageCount` | `int` | Current total number of times the coupon has been used. |
| `IsEnabled` | `bool` | Whether the coupon is currently enabled for use. |
| `UsageLimitPerUser` | `int?` | Optional per-user usage limit (enforced externally). |

### 4.2. Public Query Methods

These methods provide information about the aggregate's state without changing it.

| Method Signature | Description |
| :--- | :--- |
| `bool IsValidForUse(DateTime? checkTime = null)` | Returns `true` if the coupon is currently valid for use (enabled, within validity period, under usage limit). |
| `bool AppliesToItem(MenuItemId menuItemId, MenuCategoryId categoryId)` | Returns `true` if the coupon applies to the specified menu item based on its scope configuration. |

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `CouponCreated` | During the `Create` factory method. | Signals that a new coupon has been successfully created with its basic details. |
| `CouponUsed` | After a successful call to `Use`. | Signals that the coupon has been applied to an order, including usage count changes. |
| `CouponEnabled` | After a successful call to `Enable`. | Signals that the coupon has been enabled for use. |
| `CouponDisabled` | After a successful call to `Disable`. | Signals that the coupon has been disabled. |
| `CouponDeleted` | After a successful call to `MarkAsDeleted`. | Signals that the coupon has been marked for deletion. |

## 6. Key Value Objects

### 6.1. CouponValue

Represents the discount value of the coupon with three possible types:

* **Percentage**: Discount as a percentage (1-100%)
* **FixedAmount**: Discount as a fixed monetary amount
* **FreeItem**: A specific menu item given for free

**Factory Methods:**

* `CouponValue.CreatePercentage(decimal percentage)` - Creates percentage-based discount
* `CouponValue.CreateFixedAmount(Money amount)` - Creates fixed amount discount  
* `CouponValue.CreateFreeItem(MenuItemId menuItemId)` - Creates free item discount

### 6.2. AppliesTo

Defines the scope of items the coupon applies to with three possible scopes:

* **WholeOrder**: Applies to the entire order
* **SpecificItems**: Applies only to specified menu items
* **SpecificCategories**: Applies only to specified menu categories

**Factory Methods:**

* `AppliesTo.CreateForWholeOrder()` - Creates whole order scope
* `AppliesTo.CreateForSpecificItems(List<MenuItemId> itemIds)` - Creates specific items scope
* `AppliesTo.CreateForSpecificCategories(List<MenuCategoryId> categoryIds)` - Creates specific categories scope

## 7. Business Rules & Invariants

### 7.1. Code Uniqueness

* Coupon codes must be unique within the scope of a restaurant (enforced by Application Service).
* Codes are automatically normalized to uppercase during creation.

### 7.2. Usage Tracking

* `CurrentTotalUsageCount` cannot exceed `TotalUsageLimit` when specified.
* The `Use()` method atomically checks limits and increments the counter.
* Per-user usage limits are tracked externally via a separate `CouponUsage` table.

### 7.3. Validity Period

* `ValidityEndDate` must always be after `ValidityStartDate`.
* Coupons cannot be used outside their validity period.

### 7.4. State Consistency

* Disabled coupons cannot be used regardless of other conditions.
* Usage limits can be adjusted but cannot be set below current usage count.

## 8. External Dependencies

The aggregate references other aggregates by ID only:

* `RestaurantId` - Links to the owning Restaurant aggregate
* `MenuItemId` - Referenced in AppliesTo for specific items and CouponValue for free items
* `MenuCategoryId` - Referenced in AppliesTo for specific categories

**Note:** Per-user usage tracking is managed externally through a simple data store with structure `(UserID, CouponID, OrderID)` and is not part of this aggregate's transactional boundary.
