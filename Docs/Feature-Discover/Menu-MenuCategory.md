## Feature Discovery & Application Layer Design

### Entities Under Design: `Menu` & `MenuCategory`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Restaurant Owner` / `Staff` | Create a new menu. | Defines a new collection for menu items, such as "Lunch Menu" or "Holiday Specials". |
| `Restaurant Owner` / `Staff` | Add categories to a menu. | Populates a menu with organizational sections like "Appetizers", "Main Courses", or "Desserts". |
| `Restaurant Owner` / `Staff` | Edit menu or category details. | Updates the names or descriptions to correct typos or reflect changes. |
| `Restaurant Owner` / `Staff` | Reorder categories within a menu. | Changes the `DisplayOrder` of categories to control how they appear to customers (e.g., move "Desserts" to the end). |
| `Restaurant Owner` / `Staff` | Enable or disable a menu. | Temporarily hides or shows an entire menu (e.g., disable the "Lunch Menu" during dinner hours). |
| `Restaurant Owner` / `Staff` | Delete a menu or category. | Permanently removes an organizational structure, which may require re-assigning child items. |
| `Customer` | View the restaurant's menu. | Reads the structured list of menus, categories, and items to decide what to order. This is a read-only use case. |
| `System (Event Handler)` | Update the denormalized menu view. | A background process that listens for any changes to menus or categories and rebuilds the fast `FullMenuView` read model. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateMenuCommand`** | `Restaurant Owner/Staff` | `RestaurantId`, `Name`, `Description` | `CreateMenuResponse(MenuId)` | Must be `Owner`/`Staff` of the `RestaurantId`. |
| **`UpdateMenuDetailsCommand`** | `Restaurant Owner/Staff` | `MenuId`, `Name`, `Description` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`ToggleMenuStatusCommand`** | `Restaurant Owner/Staff` | `MenuId`, `IsEnabled` (bool) | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`DeleteMenuCommand`** | `Restaurant Owner/Staff` | `MenuId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`CreateMenuCategoryCommand`** | `Restaurant Owner/Staff` | `MenuId`, `Name`, `DisplayOrder` | `CreateMenuCategoryResponse(MenuCategoryId)` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`UpdateMenuCategoryDetailsCommand`** | `Restaurant Owner/Staff` | `MenuCategoryId`, `Name`, `DisplayOrder` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`ReorderMenuCategoriesCommand`** | `Restaurant Owner/Staff` | `MenuId`, `List<CategoryOrderDto(MenuCategoryId, NewDisplayOrder)>` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`DeleteMenuCategoryCommand`** | `Restaurant Owner/Staff` | `MenuCategoryId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetFullMenuQuery`** | `Customer` | `RestaurantId` | `FullMenuDto` (deeply nested object with menus, categories, and items) | `SELECT * FROM "FullMenuView" WHERE "RestaurantId" = @Id`. Hits the pre-compiled, denormalized read model for maximum speed. |
| **`GetMenusForManagementQuery`** | `Restaurant Owner/Staff` | `RestaurantId` | `List<MenuManagementDto>` (each DTO contains a list of `MenuCategoryManagementDto`s) | `SELECT ... FROM "Menus" m LEFT JOIN "MenuCategories" mc ON m.Id = mc.MenuId WHERE m.RestaurantId = @Id ORDER BY m.Name, mc.DisplayOrder` |
| **`GetMenuCategoryDetailsQuery`**| `Restaurant Owner/Staff` | `MenuCategoryId` | `MenuCategoryDetailsDto` | `SELECT * FROM "MenuCategories" WHERE "Id" = @Id` |

---

### 4. Domain Event Handling

*Note: The primary side effect for all these events is to update the denormalized `FullMenuView` read model to ensure customers always see the latest structure.*

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| `MenuCreated`, `MenuEnabled`, `MenuDisabled`, `MenuRemoved`, `MenuCategoryAdded`, `MenuCategoryNameUpdated`, `MenuCategoryDisplayOrderUpdated`, `MenuCategoryRemoved` | All write commands | **`UpdateFullMenuViewHandler`** | Subscribes to all relevant events. When triggered, it invalidates the cache and/or rebuilds the denormalized `FullMenuView` JSON document/tables for the affected restaurant. |
| `MenuRemoved` | `DeleteMenuCommand` | **`HandleOrphanedMenuCategories`** | A process manager that finds all `MenuCategory` entities belonging to the removed menu and marks them for deletion, which in turn raises `MenuCategoryRemoved` events. |
| `MenuCategoryRemoved` | `DeleteMenuCategoryCommand` | **`HandleOrphanedMenuItems`** | A process manager that finds all `MenuItem` aggregates belonging to the removed category. It could either mark them as deleted or re-assign them to a default "Uncategorized" category. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`ReorderMenuCategoriesCommandHandler` Orchestration:**

1.  **Validate** the command's input: The list of category orders must not be empty.
2.  **Authorize** the request:
    *   Retrieve the current user's `UserId`.
    *   Fetch the `Menu` entity using the `MenuId` from the command.
    *   Execute `CheckUserPermissionQuery` with the user's ID and the menu's `RestaurantId` to ensure they are an `Owner` or `Staff`. If not, return `Forbidden`.
3.  **Start a transaction** using `IUnitOfWork`.
4.  **Fetch required entities:**
    *   Get all `MenuCategory` entities for the given `MenuId` from the repository. This is more efficient than loading them one by one.
5.  **Perform pre-invocation business checks in the handler:**
    *   Create a dictionary or map of the incoming `(CategoryId, NewDisplayOrder)` DTOs for quick lookup.
    *   Verify that all `MenuCategoryId`s from the command input actually belong to the fetched list of categories for the menu. If an invalid ID is present, return an error.
6.  **(Optional) Use a Domain Service:** Not required here.
7.  **Invoke the Entity Methods:**
    *   Loop through the fetched `MenuCategory` entities.
    *   For each category, find its new display order from the input map.
    *   If the order has changed, call `category.UpdateDisplayOrder(newOrder)`.
    *   If `result.IsFailure`, stop and return the failure.
8.  **Persist the entities:**
    *   `await _menuCategoryRepository.UpdateRangeAsync(changedCategories);`
9.  **Complete the transaction.** The `UnitOfWork` will commit all changes and publish multiple `MenuCategoryDisplayOrderUpdated` events.
10. **Map and return** `Result.Success()`.

---

### Design Notes & Suggestions

1.  **Batch Reordering:** The `ReorderMenuCategoriesCommand` is designed to handle a batch update, which is far more efficient for a drag-and-drop UI than sending one request per category move. This is a great example of the Application Layer providing convenience on top of the Domain Layer's granular methods.
2.  **Uniqueness of Names:** The domain model does not seem to enforce uniqueness for `Menu.Name` (per restaurant) or `MenuCategory.Name` (per menu). This is a business rule that **should be enforced in the Application Layer**. Before creating a new `Menu` or `MenuCategory`, the corresponding command handler must query a read model to check if an entity with the same name already exists in that scope.
3.  **Cascading Deletes and Orphaned Entities:** Deleting a `Menu` or `MenuCategory` has significant downstream consequences. The design correctly identifies the need for event handlers (`HandleOrphanedMenuCategories`, `HandleOrphanedMenuItems`) to manage this. The business must decide on the desired behavior:
    *   **Strict Cascade:** Deleting a menu deletes all its categories, which in turn deletes all their items. This is clean but potentially destructive.
    *   **Re-assignment:** Deleting a category moves its items to a special, system-managed "Uncategorized" category for that menu. This is safer but requires more logic. This decision must be clarified and implemented in the event handlers.