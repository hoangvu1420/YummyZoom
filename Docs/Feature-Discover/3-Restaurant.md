## Feature Discovery & Application Layer Design

### Aggregate Under Design: `Restaurant`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Prospective Restaurant Owner` | Register a new restaurant. | Submits a complete profile (name, location, hours, etc.) to create a new restaurant entry in the system. The restaurant starts as unverified. |
| `Restaurant Owner` / `Staff` | Update restaurant profile details. | Modifies core information like the name, description, cuisine type, logo, or business hours to keep it current. |
| `Restaurant Owner` / `Staff` | Manage operational status. | Toggles the restaurant's ability to accept orders (e.g., opening for the day or closing due to being busy). |
| `Admin` | Verify a new restaurant. | Reviews a submitted restaurant profile and marks it as "verified," making it visible and fully operational on the platform. |
| `Admin` | Deactivate or delete a restaurant. | Marks a restaurant for deletion, either gracefully or forcefully, as part of platform governance. |
| `Customer` | View restaurant information. | Browses a restaurant's profile page to see its details, location, hours, and cuisine before ordering. |
| `System (Event Handler)` | Update search index. | A background process that listens for changes to restaurant data (name, location, cuisine) and updates the search engine index accordingly. |
| `System (Event Handler)` | Create dependent resources. | Upon restaurant creation, the system automatically creates related entities like a `RestaurantAccount` and the initial `RoleAssignment` for the owner. |

---

### 2. Commands (Write Operations)

*Note: Commands are designed to align with user-facing features, sometimes grouping multiple aggregate method calls into a single, cohesive use case.*

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateRestaurantCommand`** | `Prospective Restaurant Owner` | `CreateRestaurantDto` (contains all fields for the `Create` factory method), `OwnerUserId` | `CreateRestaurantResponse(RestaurantId)` | Must be an authenticated user. |
| **`UpdateRestaurantProfileCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `Name`, `Description`, `CuisineType` | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant or `Admin`. |
| **`UpdateRestaurantBrandingCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `LogoUrl` | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant or `Admin`. |
| **`UpdateRestaurantLocationCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `AddressDto` | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant or `Admin`. |
| **`UpdateRestaurantContactInfoCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `ContactInfoDto` | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant or `Admin`. |
| **`UpdateRestaurantHoursCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `BusinessHours` (string or structured object) | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant or `Admin`. |
| **`ToggleAcceptingOrdersCommand`** | `Restaurant Owner`, `Staff` | `RestaurantId`, `IsAccepting` (bool) | `Result.Success()` | Must be `Owner`/`Staff` of the restaurant. |
| **`VerifyRestaurantCommand`** | `Admin` | `RestaurantId` | `Result.Success()` | Must be `Admin`. |
| **`DeleteRestaurantCommand`** | `Admin` | `RestaurantId`, `ForceDelete` (bool) | `Result.Success()` | Must be `Admin`. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetRestaurantDetailsForCustomerQuery`** | `Customer` | `RestaurantId` | `RestaurantDetailsDto` (includes profile, hours, and average rating) | `SELECT r.*, rs.AverageRating, rs.TotalRatingCount FROM "Restaurants" r LEFT JOIN "RestaurantReviewSummary" rs ON r.Id = rs.RestaurantId WHERE r.Id = @Id` |
| **`GetRestaurantManagementDetailsQuery`** | `Restaurant Owner`, `Admin` | `RestaurantId` | `RestaurantManagementDto` (includes all profile data, plus `IsVerified` and `IsAcceptingOrders` status) | `SELECT * FROM "Restaurants" WHERE "Id" = @Id` |
| **`SearchRestaurantsQuery`** | `Customer` | `SearchTerm`, `LocationBounds`, `CuisineFilter`, `PaginationParameters` | `PaginatedList<RestaurantSearchResultDto>` | Utilizes the `RestaurantSearchIndex` read model (e.g., Elasticsearch) for full-text and geo-spatial queries. |
| **`IsRestaurantAcceptingOrdersQuery`**| `System (Order Service)` | `RestaurantId` | `bool` | `SELECT "IsAcceptingOrders" FROM "Restaurants" WHERE "Id" = @Id`. Highly optimized for frequent checks. |
| **`GetRestaurantsForAdminDashboardQuery`** | `Admin` | `FilterByStatus` (e.g., "PendingVerification"), `PaginationParameters` | `PaginatedList<RestaurantAdminSummaryDto>` | `SELECT Id, Name, Email, IsVerified, CreatedAt FROM "Restaurants" WHERE ... ORDER BY CreatedAt DESC` |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| **`RestaurantCreated`** | `CreateRestaurantCommand` | `AssignInitialOwnerRole` | Dispatches an `AssignRoleCommand` to create the first `Owner` `RoleAssignment` linking the `RestaurantId` to the creator's `UserId`. |
| **`RestaurantCreated`** | `CreateRestaurantCommand` | `CreateRestaurantAccount` | Finds or creates the `RestaurantAccount` aggregate for the new restaurant, initializing its balance to zero. |
| **`RestaurantVerified`** | `VerifyRestaurantCommand` | `NotifyOwnerOnRestaurantVerified` | Sends an email to the restaurant owner(s) informing them their profile is now live on the platform. |
| **`RestaurantVerified`** | `VerifyRestaurantCommand` | `UpdateRestaurantSearchIndex` | Updates the restaurant's document in the search index, potentially boosting its visibility now that it's verified. |
| `RestaurantNameChanged`, `RestaurantLocationChanged`, `RestaurantCuisineTypeChanged` | `UpdateRestaurant...` commands | `UpdateRestaurantSearchIndex` | Subscribes to multiple events to keep the search index (`RestaurantSearchIndex` read model) consistent with the restaurant's core searchable attributes. |
| **`RestaurantDeleted`** | `DeleteRestaurantCommand` | `RemoveRestaurantFromSearchIndex` | Deletes the restaurant's document from the search index. |
| **`RestaurantDeleted`** | `DeleteRestaurantCommand` | `ArchiveRelatedRestaurantData` | A saga or process manager that orchestrates the soft-deletion or archival of related data (Menus, MenuItems, Coupons, RoleAssignments). |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`CreateRestaurantCommandCommandHandler` Orchestration:**

1.  **Validate** the `CreateRestaurantDto` using FluentValidation (e.g., `Name` is not empty, `Email` is valid, `Address` fields are populated).
2.  **Authorize** the request. The invoker must be an authenticated user.
3.  **Start a transaction** using `IUnitOfWork.ExecuteInTransactionAsync`.
4.  **Fetch required data:** None needed for creation.
5.  **Perform pre-invocation business checks in the handler:**
    *   To prevent obvious duplicates, the handler could query a read model to see if a restaurant with the same name and address already exists. `if (await _restaurantLookup.ExistsAsync(command.Name, command.AddressDto)) return Failure(...)`. This is an application-level concern.
6.  **(Optional) Use a Domain Service:** Not required here.
7.  **Invoke the Aggregate's Method:**
    *   Call the static factory method with all the parameters from the DTO: `var creationResult = Restaurant.Create(...)`.
    *   If `creationResult.IsFailure`, return the result immediately (e.g., `RestaurantErrors.NameTooLong`).
8.  **Persist the new aggregate:**
    *   `await _restaurantRepository.AddAsync(creationResult.Value);`
9.  **Complete the transaction.** The `UnitOfWork` will commit the new restaurant to the database and publish the `RestaurantCreated` domain event.
10. **Map and return** the `CreateRestaurantResponse` DTO, containing the `Id` of the newly created restaurant. The event handlers (`AssignInitialOwnerRole`, `CreateRestaurantAccount`, etc.) will be triggered and executed in separate, subsequent transactions.

---

### Design Notes & Suggestions

1.  **Composite Commands:** The design proposes commands like `UpdateRestaurantProfileCommand` that group related fields. This aligns better with how a user would interact with a form ("Save Profile") and reduces API chattiness compared to having one command per field. The aggregate still maintains granular methods, offering flexibility if needed in the future.
2.  **Event Granularity:** The aggregate implementation document lists a large number of specific `...Changed` events. This is excellent for building reactive read models. However, it also lists composite events like `RestaurantBrandingUpdated`. It's recommended to clarify if the aggregate should raise *both* the granular and the composite event, or just the granular ones. Raising only granular events is often cleaner, as handlers can subscribe to the specific changes they care about.
3.  **Event-Driven Creation Flow:** The `RestaurantCreated` event is critical. The design to have it trigger the creation of the `RoleAssignment` and `RestaurantAccount` is a prime example of loose coupling. It ensures the `Restaurant` aggregate's transaction is small and focused, while subsequent, related actions are handled independently.
