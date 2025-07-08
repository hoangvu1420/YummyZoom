# Aggregate Documentation: `MenuItem`

* **Version:** 1.1
* **Last Updated:** 2025-07-07
* **Source File:** `e:\source\repos\CA\YummyZoom\src\Domain\MenuItemAggregate\MenuItem.cs`

## 1. Overview

**Description:**
*The primary transactional boundary for a single saleable item. This allows for frequent, high-performance updates (e.g., changing availability) without loading an entire menu. It manages all aspects of a menu item including pricing, availability, categorization, and customization options.*

**Core Responsibilities:**

* Manages the lifecycle of a single menu item.
* Acts as the transactional boundary for all menu item operations.
* Enforces business rules for pricing (base price cannot be negative).
* Manages item availability status for real-time inventory control.
* Associates items with dietary tags and customization groups.

## 2. Structure

* **Aggregate Root:** `MenuItem`
* **Key Child Entities:** None
* **Key Value Objects:**
  * `MenuItemId`: The unique identifier for the `MenuItem` aggregate.
  * `Money`: Represents the monetary value and currency for the base price.
  * `AppliedCustomization`: Represents a reference to a customization group with display information.
  * `RestaurantId`: A reference to the owning `Restaurant` aggregate.
  * `MenuCategoryId`: A reference to the parent `MenuCategory` entity.
  * `TagId`: A reference to dietary classification tags.

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `MenuItem` is through its static factory method.

```csharp
public static Result<MenuItem> Create(
    RestaurantId restaurantId,
    MenuCategoryId menuCategoryId,
    string name,
    string description,
    Money basePrice,
    string? imageUrl = null,
    bool isAvailable = true,
    List<TagId>? dietaryTagIds = null,
    List<AppliedCustomization>? appliedCustomizations = null
)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantId` | `RestaurantId` | The ID of the restaurant that owns this menu item. |
| `menuCategoryId` | `MenuCategoryId` | The ID of the menu category this item belongs to. |
| `name` | `string` | The name of the menu item. |
| `description` | `string` | A description of the menu item. |
| `basePrice` | `Money` | The base price of the item. |
| `imageUrl` | `string?` | Optional URL to an image of the item. |
| `isAvailable` | `bool` | Whether the item is available (defaults to true). |
| `dietaryTagIds` | `List<TagId>?` | Optional list of dietary tags for the item. |
| `appliedCustomizations` | `List<AppliedCustomization>?` | Optional list of customization groups. |

**Validation Rules & Potential Errors:**

* `name` cannot be null or whitespace. (Returns `MenuItemErrors.InvalidName`)
* `description` cannot be null or whitespace. (Returns `MenuItemErrors.InvalidDescription`)
* `basePrice.Amount` must be positive. (Returns `MenuItemErrors.NegativePrice`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `void MarkAsUnavailable()` | Marks the item as unavailable. | None. | None. |
| `void MarkAsAvailable()` | Marks the item as available. | None. | None. |
| `Result UpdateDetails(string name, string description)` | Updates the item's name and description. | Name and description must not be null or whitespace. | `MenuItemErrors.InvalidName`, `MenuItemErrors.InvalidDescription` |
| `Result UpdatePrice(Money newPrice)` | Updates the item's base price. | Price must be positive. | `MenuItemErrors.NegativePrice` |
| `Result AssignToCategory(MenuCategoryId newCategoryId)` | Moves the item to a different category. | None. | None. |
| `Result AssignCustomizationGroup(AppliedCustomization customization)` | Assigns a customization group to the item if not already present. | Customization cannot be null, group must not already be assigned. | `MenuItemErrors.CustomizationAlreadyAssigned` |
| `Result RemoveCustomizationGroup(CustomizationGroupId groupId)` | Removes a customization group from the item. | Customization group must be currently assigned. | `MenuItemErrors.CustomizationNotFound` |
| `Result SetDietaryTags(List<TagId>? tagIds)` | Replaces the entire dietary tags collection. | None (null or empty list is valid). | None. |
| `Result MarkAsDeleted()` | Marks the item as deleted. | None. | None. |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `MenuItemId` | The unique identifier of the aggregate. |
| `RestaurantId` | `RestaurantId` | The ID of the restaurant that owns this item. |
| `MenuCategoryId` | `MenuCategoryId` | The ID of the menu category this item belongs to. |
| `Name` | `string` | The item's name. |
| `Description` | `string` | The item's description. |
| `BasePrice` | `Money` | The base price of the item. |
| `ImageUrl` | `string?` | Optional URL to an image of the item. |
| `IsAvailable` | `bool` | Whether the item is currently available. |
| `DietaryTagIds` | `IReadOnlyList<TagId>` | A read-only list of dietary tags associated with the item. |
| `AppliedCustomizations` | `IReadOnlyList<AppliedCustomization>` | A read-only list of customization groups applied to the item. |

### 4.2. Public Query Methods

This aggregate does not expose any public query methods beyond its properties.

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `MenuItemCreated` | During the `Create` factory method. | Signals that a new menu item has been successfully created. |
| `MenuItemAvailabilityChanged` | After a successful call to `MarkAsAvailable` or `MarkAsUnavailable`. | Signals that the item's availability status has changed. |
| `MenuItemPriceChanged` | After a successful call to `UpdatePrice`. | Signals that the item's price has been updated. |
| `MenuItemAssignedToCategory` | After a successful call to `AssignToCategory`. | Signals that the item has been moved to a different category. |
| `MenuItemCustomizationAssigned` | After a successful call to `AssignCustomizationGroup`. | Signals that a customization group has been assigned to the item. |
| `MenuItemCustomizationRemoved` | After a successful call to `RemoveCustomizationGroup`. | Signals that a customization group has been removed from the item. |
| `MenuItemDietaryTagsUpdated` | After a successful call to `SetDietaryTags`. | Signals that the item's dietary tags have been updated. |
| `MenuItemDeleted` | After a successful call to `MarkAsDeleted`. | Signals that the item has been marked for deletion. |
