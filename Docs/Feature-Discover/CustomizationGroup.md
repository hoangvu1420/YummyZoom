## Feature Discovery & Application Layer Design

### Aggregate Under Design: `CustomizationGroup`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Restaurant Owner` / `Staff` | Create a reusable set of options. | Defines a new group like "Pizza Toppings" or "Steak Doneness" with rules for how many options can be selected. |
| `Restaurant Owner` / `Staff` | Add/Edit/Remove specific options. | Manages the individual choices within a group, such as adding "Extra Cheese" or changing the price adjustment for "Bacon". |
| `Restaurant Owner` / `Staff` | Update the rules for a group. | Changes the `minSelections` or `maxSelections` for a group (e.g., "Choose up to 3 toppings" instead of 2). |
| `Restaurant Owner` / `Staff` | Control the display order of choices. | Arranges choices in a logical order for the customer (e.g., "Small, Medium, Large" instead of alphabetical). |
| `Restaurant Owner` / `Staff` | Apply a group to a menu item. | Links this `CustomizationGroup` to one or more `MenuItem` aggregates. (This action is performed via a `MenuItem` command). |
| `Restaurant Owner` / `Staff` | Delete an entire option set. | Marks a `CustomizationGroup` as deleted when it's no longer needed. |
| `System (Event Handler)` | Update affected menu item views. | When a group or choice is changed, a background process must update the `FullMenuView` read model for all `MenuItem`s that use this group. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateCustomizationGroupCommand`** | `Restaurant Owner/Staff` | `RestaurantId`, `GroupName`, `Min`, `Max` | `CreateCustomizationGroupResponse(GroupId)` | Must be `Owner`/`Staff` of the `RestaurantId`. |
| **`UpdateCustomizationGroupDetailsCommand`** | `Restaurant Owner/Staff` | `GroupId`, `GroupName`, `Min`, `Max` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`AddChoiceToGroupCommand`** | `Restaurant Owner/Staff` | `GroupId`, `ChoiceName`, `PriceAdjustment`, `IsDefault` | `AddChoiceToGroupResponse(ChoiceId)` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`UpdateChoiceInGroupCommand`** | `Restaurant Owner/Staff` | `GroupId`, `ChoiceId`, `NewName`, `NewPrice`, `IsDefault` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`RemoveChoiceFromGroupCommand`** | `Restaurant Owner/Staff` | `GroupId`, `ChoiceId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`ReorderChoicesInGroupCommand`** | `Restaurant Owner/Staff` | `GroupId`, `List<ChoiceOrderDto(ChoiceId, NewOrder)>` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |
| **`DeleteCustomizationGroupCommand`** | `Restaurant Owner/Staff` | `GroupId` | `Result.Success()` | Must be `Owner`/`Staff` of the associated restaurant. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetCustomizationGroupsForRestaurantQuery`** | `Restaurant Owner/Staff` | `RestaurantId` | `List<CustomizationGroupSummaryDto>` | `SELECT Id, GroupName, (SELECT COUNT(*) FROM "Choices" WHERE GroupId = cg.Id) as ChoiceCount FROM "CustomizationGroups" cg WHERE RestaurantId = @Id` |
| **`GetCustomizationGroupDetailsQuery`** | `Restaurant Owner/Staff` | `GroupId` | `CustomizationGroupDetailsDto` | `SELECT * FROM "CustomizationGroups" cg LEFT JOIN "Choices" c ON cg.Id = c.GroupId WHERE cg.Id = @Id ORDER BY c.DisplayOrder` |
| **`GetCustomizationGroupsForAssignmentQuery`**| `Restaurant Owner/Staff` | `RestaurantId` | `List<GroupAssignmentDto(Id, Name)>` | A lightweight query to populate a dropdown/multi-select when editing a `MenuItem`. `SELECT Id, GroupName FROM "CustomizationGroups" WHERE RestaurantId = @Id` |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| `CustomizationGroupCreated`, `CustomizationChoiceAdded`, `CustomizationChoiceRemoved`, `CustomizationChoiceUpdated` | All write commands | **`UpdateFullMenuViewOnCustomizationChangeHandler`** | Subscribes to all relevant events. Finds all `MenuItem`s that use the affected `CustomizationGroup` and triggers a rebuild of their `FullMenuView` read model entry. |
| `CustomizationGroupDeleted` | `DeleteCustomizationGroupCommand` | **`DisassociateGroupFromMenuItemsHandler`** | A critical process manager. It finds all `MenuItem` aggregates that reference the deleted `CustomizationGroupId` and calls a method on each to remove the association, raising further events. |
| `CustomizationGroupDeleted` | `DeleteCustomizationGroupCommand` | **`UpdateFullMenuViewOnCustomizationChangeHandler`** | The same handler as above will also react to the deletion, ensuring the group is removed from all customer-facing views. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`CreateCustomizationGroupCommandHandler` Orchestration:**

1.  **Validate** command input: `GroupName` is required, `Min` and `Max` are valid numbers.
2.  **Authorize** the request: Check if the current user has `Owner`/`Staff` rights for the `RestaurantId`.
3.  **Start a transaction** using `IUnitOfWork`.
4.  **Fetch required entities:** None needed for a simple creation.
5.  **Perform pre-invocation business checks in the handler:**
    *   **Uniqueness Invariant:** The aggregate itself doesn't know about other groups. The application service must enforce this rule.
    *   `var nameExists = await _groupRepository.DoesNameExistForRestaurantAsync(command.GroupName, command.RestaurantId)`.
    *   If `true`, return `CustomizationGroupErrors.GroupNameNotUnique` error.
6.  **(Optional) Use a Domain Service:** Not required.
7.  **Invoke the Aggregate's Method:**
    *   `var creationResult = CustomizationGroup.Create(command.RestaurantId, command.GroupName, ...);`
    *   If `creationResult.IsFailure`, return the result (e.g., `CustomizationGroupErrors.InvalidSelectionRange`).
8.  **Persist the new aggregate:**
    *   `await _groupRepository.AddAsync(creationResult.Value);`
9.  **Complete the transaction.** The `UnitOfWork` commits and dispatches the `CustomizationGroupCreated` event.
10. **Map and return** the `CreateCustomizationGroupResponse` DTO.

---

### Design Notes & Suggestions

1.  **CRITICAL GAPI - Choice Ordering:** The provided aggregate documentation is missing a way to order the `CustomizationChoice` entities. For a group like "Size," the owner must be able to enforce the order "Small, Medium, Large."
    *   **Recommendation:**
        1.  Add a `DisplayOrder` property to the `CustomizationChoice` entity.
        2.  Create a new command, `ReorderChoicesInGroupCommand`, that accepts a list of `(ChoiceId, NewDisplayOrder)` pairs.
        3.  The command handler for this will fetch the aggregate, loop through the choices, and call a new method on the `CustomizationChoice` entity like `UpdateDisplayOrder(int order)`.

2.  **CRITICAL CONSIDERATION - Deletion Strategy:** What happens when a `CustomizationGroup` is deleted but is currently used by 10 `MenuItem`s?
    *   **Bad Approach:** Fail the delete command. This forces the user to manually go to all 10 items and un-assign the group first. This is a very poor user experience.
    *   **Recommended Approach (Decoupled via Events):** The `DeleteCustomizationGroupCommand` should always succeed in marking the group as deleted. The `CustomizationGroupDeleted` event then triggers the `DisassociateGroupFromMenuItemsHandler`. This handler is responsible for loading each affected `MenuItem` aggregate and calling its `RemoveCustomizationGroup(deletedGroupId)` method. This is a robust, background-safe, and user-friendly solution that correctly separates concerns.

3.  **Enriching the `AddChoice` Method:** The aggregate's `AddChoice` method takes a fully-formed `CustomizationChoice` entity. A more convenient and safer approach for the command handler would be to have a method signature like: `Result AddChoice(string name, Money priceAdjustment, bool isDefault, int displayOrder)`.
    *   **Recommendation:** The `AddChoice` method on the aggregate root should be responsible for *creating* the `CustomizationChoice` entity itself. This encapsulates the creation logic and ensures the new choice is immediately associated with its parent. The command handler would then just pass primitive values.

4.  **Bulk Operations for UI Convenience:** Restaurant staff will likely want to add several choices at once.
    *   **Consideration:** While the design with single-add commands is correct from a granular DDD perspective, consider adding a bulk command like `AddChoicesToGroupCommand` which takes a `List<ChoiceDto>`. The handler would loop through this list, calling the aggregate's `AddChoice` method for each one within a single transaction. This significantly improves performance and UX for a "quick add" UI feature.

---

### Summary of Critical Considerations & Suggestions

#### 1. Implement Choice Ordering (Critical Feature Gap)
*   **Problem:** The current design for `CustomizationChoice` lacks a `DisplayOrder` property. This means options within a group (e.g., "Small", "Medium", "Large") cannot be presented in a specific, logical order and will likely appear alphabetically or randomly, which is a significant flaw in the user experience.
*   **Suggestion:**
    1.  Add a `DisplayOrder` (int) property to the `CustomizationChoice` entity.
    2.  Create a new `ReorderChoicesInGroupCommand` in the application layer that accepts a list of `(ChoiceId, NewDisplayOrder)` pairs. This allows a user interface to efficiently update the order of all choices in a group with a single request.

#### 2. Decouple Deletion Logic for In-Use Groups (System Robustness)
*   **Problem:** Deleting a `CustomizationGroup` that is already applied to multiple menu items is a dangerous operation. Blocking the deletion creates a terrible user experience (forcing the user to manually find and edit every item), while simply deleting it creates orphaned data and potential errors.
*   **Suggestion:** Implement an event-driven, decoupled deletion process:
    1.  Allow the `DeleteCustomizationGroupCommand` to always succeed in marking the group as deleted.
    2.  When the `CustomizationGroupDeleted` event is raised, a dedicated background event handler (`DisassociateGroupFromMenuItemsHandler`) must find all `MenuItem` aggregates that use this group.
    3.  This handler then loads each affected `MenuItem` and calls a method on it to remove the reference to the now-deleted group. This ensures data consistency without blocking the user's action.

#### 3. Strengthen Aggregate Encapsulation
*   **Problem:** The `AddChoice` method on the aggregate root expects a fully-formed `CustomizationChoice` object as a parameter. This forces the creation logic for the child entity into the application layer, weakening the aggregate's boundary.
*   **Suggestion:** Refine the `AddChoice` method signature on the `CustomizationGroup` aggregate to accept primitive types: `Result AddChoice(string name, Money priceAdjustment, bool isDefault)`. The aggregate root should be solely responsible for creating its own child entities, ensuring all invariants are checked within its transactional boundary.

#### 4. Support Bulk Operations for Better UX and Performance
*   **Problem:** The design provides commands for adding/editing one choice at a time. For a user interface where a restaurant owner wants to add a dozen toppings to a "Pizza Toppings" group, making a dozen separate network requests is highly inefficient and provides a poor user experience.
*   **Suggestion:** Create application-level "bulk" commands, such as `AddChoicesToGroupCommand`, that accept a list of DTOs. The command handler will load the `CustomizationGroup` aggregate *once*, and then loop through the list, calling the aggregate's single `AddChoice` method for each item within a single database transaction. This greatly improves performance and usability without complicating the domain model itself.