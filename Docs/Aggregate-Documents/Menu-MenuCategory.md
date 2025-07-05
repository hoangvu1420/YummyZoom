# Entity Documentation: `Menu` & `MenuCategory`

* **Version:** 1.0
* **Last Updated:** 2025-07-05
* **Source Files:**
  * `e:\source\repos\CA\YummyZoom\src\Domain\MenuEntity\Menu.cs`
  * `e:\source\repos\CA\YummyZoom\src\Domain\MenuEntity\MenuCategory.cs`

## 1. Overview

**Description:**
*Independent domain entities that serve as organizational tools for structuring restaurant menu data. Following performance analysis, these were split from a monolithic Menu aggregate to serve as lightweight organizational entities rather than consistency boundaries.*

**Core Responsibilities:**

* **Menu**: Groups MenuCategories into named collections (e.g., "Lunch Menu", "All Day Menu").
* **MenuCategory**: Groups MenuItems into sections (e.g., "Appetizers", "Desserts").
* Both entities manage their own lifecycle and state transitions.
* They do not contain collections of their children; relationships are maintained via ID references.

## 2. Structure

### 2.1. Menu Entity

* **Entity Root:** `Menu`
* **Key Value Objects:**
  * `MenuId`: The unique identifier for the `Menu` entity.
  * `RestaurantId`: A reference to the owning `Restaurant` aggregate.

### 2.2. MenuCategory Entity

* **Entity Root:** `MenuCategory`
* **Key Value Objects:**
  * `MenuCategoryId`: The unique identifier for the `MenuCategory` entity.
  * `MenuId`: A reference to the parent `Menu` entity.

## 3. Lifecycle & State Management

### 3.1. Menu Creation (Factory Method)

```csharp
public static Result<Menu> Create(
    RestaurantId restaurantId,
    string name,
    string description,
    bool isEnabled = true
)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantId` | `RestaurantId` | The ID of the restaurant that owns this menu. |
| `name` | `string` | The name of the menu. |
| `description` | `string` | A description of the menu. |
| `isEnabled` | `bool` | Whether the menu is enabled (defaults to true). |

**Validation Rules & Potential Errors:**

* `name` cannot be null or whitespace. (Returns `MenuErrors.InvalidMenuName`)
* `description` cannot be null or whitespace. (Returns `MenuErrors.InvalidMenuDescription`)

### 3.2. MenuCategory Creation (Factory Method)

```csharp
public static Result<MenuCategory> Create(MenuId menuId, string name, int displayOrder)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `menuId` | `MenuId` | The ID of the menu this category belongs to. |
| `name` | `string` | The name of the category. |
| `displayOrder` | `int` | The display order within the menu. |

**Validation Rules & Potential Errors:**

* `name` cannot be null or whitespace. (Returns `MenuErrors.InvalidCategoryName`)
* `displayOrder` must be a positive number. (Returns `MenuErrors.InvalidDisplayOrder`)

### 3.3. State Transitions & Commands (Public Methods)

#### Menu Methods

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result UpdateDetails(string name, string description)` | Updates the menu's name and description. | Name and description must not be null or whitespace. | `MenuErrors.InvalidMenuName`, `MenuErrors.InvalidMenuDescription` |
| `void Enable()` | Enables the menu if it's currently disabled. | None. | None. |
| `void Disable()` | Disables the menu if it's currently enabled. | None. | None. |
| `Result MarkAsDeleted(bool forceDelete = false)` | Marks the menu as deleted. | None. | None. |

#### MenuCategory Methods

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result UpdateName(string name)` | Updates the category's name. | Name must not be null or whitespace. | `MenuErrors.InvalidCategoryName` |
| `Result UpdateDisplayOrder(int displayOrder)` | Updates the category's display order. | Display order must be positive. | `MenuErrors.InvalidDisplayOrder` |
| `Result MarkAsDeleted(bool forceDelete = false)` | Marks the category as deleted. | None. | None. |

## 4. Exposed State & Queries

### 4.1. Menu Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `MenuId` | The unique identifier of the menu. |
| `Name` | `string` | The menu's name. |
| `Description` | `string` | The menu's description. |
| `IsEnabled` | `bool` | Whether the menu is currently enabled. |
| `RestaurantId` | `RestaurantId` | The ID of the restaurant that owns this menu. |

### 4.2. MenuCategory Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `MenuCategoryId` | The unique identifier of the category. |
| `MenuId` | `MenuId` | The ID of the menu this category belongs to. |
| `Name` | `string` | The category's name. |
| `DisplayOrder` | `int` | The display order within the menu. |

## 5. Communication (Domain Events)

### 5.1. Menu Events

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `MenuCreated` | During the `Create` factory method. | Signals that a new menu has been successfully created. |
| `MenuEnabled` | After a successful call to `Enable`. | Signals that the menu has been enabled. |
| `MenuDisabled` | After a successful call to `Disable`. | Signals that the menu has been disabled. |
| `MenuRemoved` | After a successful call to `MarkAsDeleted`. | Signals that the menu has been marked for deletion. |

### 5.2. MenuCategory Events

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `MenuCategoryAdded` | During the `Create` factory method. | Signals that a new menu category has been successfully created. |
| `MenuCategoryNameUpdated` | After a successful call to `UpdateName`. | Signals that the category's name has been updated. |
| `MenuCategoryDisplayOrderUpdated` | After a successful call to `UpdateDisplayOrder`. | Signals that the category's display order has been updated. |
| `MenuCategoryRemoved` | After a successful call to `MarkAsDeleted`. | Signals that the category has been marked for deletion. |
