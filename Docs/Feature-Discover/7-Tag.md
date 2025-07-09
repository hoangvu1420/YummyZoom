## Feature Discovery & Application Layer Design

### Entity Under Design: `Tag`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Admin` | Create a new system-wide tag. | Defines a new, curated tag (e.g., "Vegan", "Halal") available for all restaurants to use. This ensures data consistency. |
| `Admin` | Edit an existing tag. | Updates a tag's name or description (e.g., correcting a typo) to propagate the change across the entire platform. |
| `Admin` | Delete a tag from the system. | Removes a tag that is obsolete or was created in error. |
| `Restaurant Owner` / `Staff`| Assign tags to a menu item. | Selects from the pre-defined list of tags to classify one of their `MenuItem`s. (This is a `MenuItem` command). |
| `Customer` | Filter restaurants or menu items. | Uses tags as filters to discover items that meet their criteria (e.g., show only "Gluten-Free" options). |
| `System (Event Handler)` | Update search indexes and views. | When a tag is updated or deleted, a background process updates all denormalized views and search indexes that reference it. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateTagCommand`** | `Admin` | `TagName`, `TagCategory`, `Description` | `CreateTagResponse(TagId)` | `Admin` role only. |
| **`UpdateTagCommand`** | `Admin` | `TagId`, `NewTagName`, `NewDescription` | `Result.Success()` | `Admin` role only. |
| **`ChangeTagCategoryCommand`** | `Admin` | `TagId`, `NewTagCategory` | `Result.Success()` | `Admin` role only. |
| **`DeleteTagCommand`** | `Admin` | `TagId` | `Result.Success()` | `Admin` role only. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetAllTagsQuery`** | `Admin`, `Restaurant Owner/Staff` | `FilterByCategory` (optional) | `List<TagDto>` | `SELECT Id, TagName, TagCategory FROM "Tags" WHERE IsDeleted = false ORDER BY TagCategory, TagName` |
| **`GetTagsForManagementQuery`** | `Admin` | `PaginationParameters` | `PaginatedList<TagManagementDto>` | `SELECT t.Id, t.TagName, t.TagCategory, COUNT(mit.MenuItemId) as UsageCount FROM "Tags" t LEFT JOIN "MenuItemTags" mit ON t.Id = mit.TagId GROUP BY t.Id` |
| **`GetTagDetailsQuery`** | `Admin` | `TagId` | `TagDetailsDto` | `SELECT * FROM "Tags" WHERE Id = @Id` |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| `TagCreated` | `CreateTagCommand` | `LogAdminActionOnTag` | Creates an audit trail entry for the creation of a new global tag. |
| `TagUpdated` | `UpdateTagCommand` | **`UpdateDenormalizedViewsOnTagChangeHandler`** | Finds all `MenuItem`s that use this `TagId` and triggers a refresh of their `FullMenuView` read model to display the new name. |
| `TagDeleted` | `DeleteTagCommand` | **`DisassociateTagFromMenuItemsHandler`** | A critical process manager. It finds all `MenuItem` aggregates that reference the deleted `TagId` and calls a method on each to remove the association, ensuring data integrity. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`UpdateTagCommandHandler` Orchestration:**

1.  **Validate** command input: `TagId` is valid, `NewTagName` is not empty and within length limits.
2.  **Authorize** the request: User must have the `Admin` role.
3.  **Start a transaction** using `IUnitOfWork`.
4.  **Fetch the entity:**
    *   `var tag = await _tagRepository.GetByIdAsync(command.TagId)`. If not found, return an error.
5.  **Perform pre-invocation business checks in the handler:**
    *   **Uniqueness Invariant:** If the name is changing, the application service must check if the *new* name is already taken.
    *   `if (tag.TagName != command.NewTagName)`
    *   `  var nameExists = await _tagRepository.DoesNameExistAsync(command.NewTagName)`
    *   `  if (nameExists) return TagErrors.NameAlreadyExists;`
6.  **(Optional) Use a Domain Service:** Not required.
7.  **Invoke the Entity's Method:**
    *   `var updateResult = tag.UpdateDetails(command.NewTagName, command.NewDescription);`
    *   If `updateResult.IsFailure`, return the result.
8.  **Persist the entity:**
    *   `await _tagRepository.UpdateAsync(tag);`
9.  **Complete the transaction.** The `UnitOfWork` commits and dispatches the `TagUpdated` event if the name was changed.
10. **Return** `Result.Success()`.

---

### Design Notes & Suggestions

1.  **CRITICAL: Governance Model - Admin-Managed Tags:** The design correctly implies that `Tag`s are master data managed exclusively by Admins. This is a crucial strategic decision.
    *   **Benefit:** It prevents data chaos. If restaurants could create their own tags, you would inevitably end up with duplicates and variations ("Veg", "Vegan", "Vegetarian", "V"), making filtering useless for customers.
    *   **Recommendation:** Solidify this by ensuring all write commands (`Create`, `Update`, `Delete`) are protected by a strict `Admin` authorization policy. The UI for restaurant owners should only ever present a multi-select list of existing, approved tags.

2.  **CRITICAL: Deletion Strategy and Referential Integrity:** What happens when an Admin tries to delete a tag that is actively used by hundreds of menu items?
    *   **Option A (Hard Block):** The `DeleteTagCommand` handler first queries a `MenuItemTags` join table. If `UsageCount > 0`, it fails the command. This is safe but creates a poor UX for the Admin, who now has to figure out which items use the tag.
    *   **Option B (Soft Delete):** The `MarkAsDeleted()` method just sets an `IsDeleted` flag. The tag remains in the database but is filtered out of all queries for new assignments. This is simple but can lead to orphaned references over time.
    *   **Recommended Approach (Decoupled Disassociation):** The `DeleteTagCommand` should permanently delete the tag record. The `TagDeleted` event triggers the `DisassociateTagFromMenuItemsHandler`. This handler runs in the background, finds all `MenuItem`s using the deleted `TagId`, and removes the reference from their `DietaryTagIds` list. This is the most robust, scalable, and user-friendly approach, as the Admin's action is immediate, and the system cleans itself up.

3.  **Future-Proofing `TagCategory`:** The documentation specifies `TagCategory` as an enum. This is fine for a fixed set of categories like "Dietary" or "Cuisine".
    *   **Consideration:** If the business might want to add new tag categories in the future *without* a code deployment, consider modeling `TagCategory` as its own simple lookup entity (`TagCategoryId`, `CategoryName`). This turns a hardcoded enum into manageable data, increasing system flexibility at the cost of a slightly more complex data model. For now, the enum is a reasonable starting point.

4.  **Enhancing Read Queries for Management:** The `Admin` will want to know how widely a tag is used before editing or deleting it.
    *   **Recommendation:** Create the `GetTagsForManagementQuery` as proposed above. It joins with the `MenuItemTags` mapping table to return a `UsageCount` alongside each tag. This gives the Admin valuable context to make informed decisions.

5.  **Event Granularity:** The `TagUpdated` event is raised only when the name changes. This is excellent. There's no need to trigger a massive cascade of read model updates if only the `Description` (which is likely not displayed in list views) is modified. This shows careful consideration for system performance.