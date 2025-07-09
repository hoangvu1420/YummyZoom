## Feature Discovery & Application Layer Design

### Aggregate Under Design: `MenuItem`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Restaurant Owner` / `Staff` | Add a new dish to the menu. | Creates a new `MenuItem` with its name, description, price, and initial availability. |
| `Restaurant Owner` / `Staff` | Update a dish's core details. | Changes the name, description, or image of an existing menu item. |
| `Restaurant Owner` / `Staff` | Change the price of a dish. | Updates the `BasePrice` of a `MenuItem`. |
| `Restaurant Owner` / `Staff` | Mark a dish as "Out of Stock". | The most frequent operation: toggling the `IsAvailable` flag to control real-time ordering. |
| `Restaurant Owner` / `Staff` | Organize the menu. | Moves a `MenuItem` from one `MenuCategory` to another. |
| `Restaurant Owner` / `Staff` | Manage dietary information. | Assigns or removes dietary tags (e.g., "Gluten-Free", "Vegetarian") to an item. |
| `Restaurant Owner` / `Staff` | Manage item options. | Applies or removes pre-defined `CustomizationGroup`s (e.g., "Sizes", "Toppings") to a menu item. |
| `Restaurant Owner` / `Staff` | Remove a dish from the menu. | Marks a `MenuItem` as deleted. |
| `System (Event Handler)` | Update customer-facing menu views. | Rebuilds the `FullMenuView` read model in response to any change in a menu item's state. |
| `System (Event Handler)` | Update search capabilities. | Updates the `RestaurantSearchIndex` when an item is created, deleted, or its name changes, allowing customers to search for specific dishes. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateMenuItemCommand`** | `Restaurant Owner/Staff` | `CreateMenuItemDto` (all params for factory) | `CreateMenuItemResponse(MenuItemId)` | Must be `Owner`/`Staff` of the `RestaurantId`. |
| **`UpdateMenuItemDetailsCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `Name`, `Description`, `ImageUrl` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`UpdateMenuItemPriceCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `NewPrice` (decimal), `Currency` (string) | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`ToggleMenuItemAvailabilityCommand`**| `Restaurant Owner/Staff` | `MenuItemId`, `IsAvailable` (bool) | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`MoveMenuItemToCategoryCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `NewMenuCategoryId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`UpdateMenuItemTagsCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `List<TagId>` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`AssignCustomizationToMenuItemCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `CustomizationGroupId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`RemoveCustomizationFromMenuItemCommand`** | `Restaurant Owner/Staff` | `MenuItemId`, `CustomizationGroupId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`DeleteMenuItemCommand`** | `Restaurant Owner/Staff` | `MenuItemId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetFullMenuQuery`** | `Customer` | `RestaurantId` | `FullMenuDto` | `SELECT * FROM "FullMenuView" WHERE "RestaurantId" = @Id`. Hits the fast read model. |
| **`GetMenuItemsForManagementQuery`** | `Restaurant Owner/Staff` | `RestaurantId`, `PaginationParameters`, `FilterByCategoryId` | `PaginatedList<MenuItemSummaryDto>` | `SELECT mi.Id, mi.Name, mi.BasePrice, mi.IsAvailable, mc.Name as CategoryName FROM "MenuItems" mi JOIN "MenuCategories" mc ON mi.MenuCategoryId = mc.Id WHERE mi.RestaurantId = @Id` |
| **`GetMenuItemDetailsForEditingQuery`**| `Restaurant Owner/Staff` | `MenuItemId` | `MenuItemDetailsDto` (includes `List<TagId>` and `List<CustomizationGroupId>`) | `SELECT * FROM "MenuItems" WHERE "Id" = @Id`. The handler would also fetch assigned Tag/Customization IDs from join tables. |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| `MenuItemCreated`, `MenuItemAvailabilityChanged`, `MenuItemPriceChanged`, `MenuItemAssignedToCategory`, `MenuItemDeleted`, and all other update events. | All write commands | **`UpdateFullMenuViewHandler`** | The primary handler. Subscribes to all `MenuItem` events to keep the denormalized customer-facing menu view perfectly in sync. |
| `MenuItemCreated`, `MenuItemDeleted`, `(MenuItemDetailsUpdated)` | `CreateMenuItemCommand`, `DeleteMenuItemCommand`, `UpdateMenuItemDetailsCommand` | **`UpdateRestaurantSearchIndexHandler`** | Adds, removes, or updates the menu item in the search index, making dishes searchable by customers. |
| `MenuItemDeleted` | `DeleteMenuItemCommand` | **`HandleMenuItemDeletionInCoupons`** | A process manager that finds any `Coupon`s configured for "Free Item" or "Discount on Specific Items" that reference the deleted `MenuItemId`. It could then disable the coupon or notify the owner. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`CreateMenuItemCommandHandler` Orchestration:**

1.  **Validate** the `CreateMenuItemDto` using FluentValidation.
2.  **Authorize** the request: Check if the current user is `Owner` or `Staff` for the `RestaurantId` in the command.
3.  **Start a transaction** using `IUnitOfWork`.
4.  **Fetch related entities for cross-aggregate validation:**
    *   `var menuCategory = await _menuCategoryRepository.GetByIdAsync(command.MenuCategoryId)`.
    *   `var tags = await _tagRepository.GetByIdsAsync(command.DietaryTagIds)`.
    *   `var customizationGroups = await _customizationGroupRepository.GetByIdsAsync(command.AppliedCustomizationGroupIds)`.
5.  **Perform pre-invocation business checks in the handler:**
    *   **Existence & Ownership:**
        *   If `menuCategory` is `null` or `menuCategory.RestaurantId != command.RestaurantId`, return `MenuCategoryNotFound` error.
        *   If the count of `tags` found doesn't match the input count, return `TagNotFound` error.
        *   If the count of `customizationGroups` doesn't match, or if any group's `RestaurantId` doesn't match the command's `RestaurantId`, return `CustomizationGroupNotFound` error.
    *   **Uniqueness Invariant:** As per `Domain_Design.md`, enforce name uniqueness within the category.
        *   `var nameExists = await _menuItemRepository.DoesNameExistInCategoryAsync(command.Name, command.MenuCategoryId)`.
        *   If `true`, return `MenuItemNameNotUnique` error.
6.  **(Optional) Use a Domain Service:** Not required here.
7.  **Invoke the Aggregate's Method:**
    *   `var creationResult = MenuItem.Create(command.RestaurantId, ...);`
    *   If `creationResult.IsFailure`, return the result (e.g., `MenuItemErrors.NegativePrice`).
8.  **Persist the new aggregate:**
    *   `await _menuItemRepository.AddAsync(creationResult.Value);`
9.  **Complete the transaction.** The `UnitOfWork` commits and dispatches the `MenuItemCreated` event.
10. **Map and return** the `CreateMenuItemResponse` DTO.

---

### Design Notes & Suggestions

1.  **CRITICAL GAPI - Managing Collections:** The provided aggregate documentation is missing methods to update the `DietaryTagIds` and `AppliedCustomizations` lists after an item is created. This is a significant omission.
    *   **Recommendation:** Add the following methods to the `MenuItem` aggregate:
        ```csharp
        // In MenuItem.cs
        public Result AssignCustomizationGroup(AppliedCustomization customization) { /* Adds to list if not present */ }
        public Result RemoveCustomizationGroup(CustomizationGroupId groupId) { /* Removes from list */ }
        public Result SetDietaryTags(List<TagId> tagIds) { /* Replaces the entire list */ }
        ```
    *   These methods would then be used by the new commands proposed above (`AssignCustomizationToMenuItemCommand`, etc.). `SetDietaryTags` is a "replace all" operation, which is often simpler and safer for managing tag-like collections from a UI.

2.  **Cross-Aggregate Validation is Key:** The orchestration for `CreateMenuItemCommand` demonstrates a critical responsibility of the Application Layer: enforcing rules that span multiple aggregates/entities *before* calling the aggregate's method. The aggregate itself should only be responsible for its own internal state. The handler correctly validates that the `MenuCategory`, `Tag`s, and `CustomizationGroup`s all exist and belong to the correct restaurant.

3.  **Richer `AppliedCustomization` Value Object:** The documentation lists `AppliedCustomization` as a VO. To improve the utility of read models and reduce joins, this VO could be enriched.
    *   **Recommendation:** Define the `AppliedCustomization` VO as:
        ```csharp
        public class AppliedCustomization : ValueObject
        {
            public CustomizationGroupId GroupId { get; private set; }
            public string GroupName { get; private set; } // Snapshot for display
            public int DisplayOrder { get; private set; } // Controls order of options on UI
        }
        ```
    *   When a customization is assigned, the command handler would fetch the `CustomizationGroup`, extract its name, and pass both the ID and the name to the `MenuItem` aggregate. This denormalizes the group's name into the `MenuItem` aggregate, simplifying the `FullMenuView` read model generation.

4.  **Image Handling Strategy:** The `ImageUrl` is a simple string. This is fine, but for a real-world system, this implies the client has already uploaded the image somewhere to get a URL.
    *   **Consideration:** A more robust flow would involve the `UpdateMenuItemDetailsCommand` accepting an image file stream. The command handler would be responsible for:
        1.  Receiving the file.
        2.  Calling an `IFileUploadService` to upload the image to cloud storage (e.g., S3, Azure Blob).
        3.  Receiving the permanent URL back from the service.
        4.  Passing this URL to the `MenuItem.UpdateDetails` method.

5.  **Price Change Auditing:** A `MenuItemPriceChanged` event is excellent. This should be treated as a high-value event. A dedicated event handler, `LogPriceChangeAudit`, could write to a separate audit table (`MenuItemPriceHistory`) to track every price change for a given item, including who changed it and when. This is crucial for financial reporting and tracking.