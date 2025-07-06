## Feature Discovery & Application Layer Design Template

### Aggregate Under Design: `[Name of the Aggregate, e.g., Order, Restaurant, Coupon]`

### 1. Core Use Cases & Actors

***Instructions:*** *Identify who (which user role or system process) interacts with this aggregate and what their primary goals are. This helps define the scope and purpose of the features.*

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `[e.g., Customer]` | `[e.g., Place a new order]` | `[e.g., The primary action of creating an instance with items, address, etc.]` |
| `[e.g., Restaurant Staff]` | `[e.g., Update menu item availability]` | `[e.g., Mark an item as "out of stock" or "available".]` |
| `[e.g., Admin]` | `[e.g., Verify a new restaurant]` | `[e.g., Manually approve a restaurant's profile after reviewing it.]` |
| `[e.g., System (Event Handler)]` | `[e.g., Increment a coupon's usage count]` | `[e.g., A background process that updates the coupon after an order is placed.]` |

---

### 2. Commands (Write Operations)

***Instructions:*** *List all actions that will create or change the state of this aggregate. Each command represents a single, atomic use case from the Application Layer's perspective.*

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| **`[Action][Aggregate]Command`** | `[e.g., Customer]` | `[e.g., AggregateId, Dto, UserId]` | `[e.g., CreateAggregateResponse(NewId)]` or `Result.Success()` | `[e.g., Customer role, must own the entity]` |
| `CreateRestaurantCommand` | `Restaurant Owner` | `Name`, `LocationDto`, `CuisineType` | `CreateRestaurantResponse(RestaurantId)` | `Restaurant Owner` role. |
| `UpdateMenuItemPriceCommand`| `Restaurant Staff` | `MenuItemId`, `NewPrice` | `Result.Success()` | `Restaurant Staff` role, must be associated with the restaurant. |
| `DeactivateUserCommand` | `Admin` | `UserId` | `Result.Success()` | `Admin` role. |

---

### 3. Queries (Read Operations)

***Instructions:*** *List all the ways data needs to be retrieved for this aggregate. Remember, queries use Dapper/SQL for performance and can join across tables to create tailored DTOs.*

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`[Get/Find][DataShape]Query`** | `[e.g., Customer]` | `[e.g., AggregateId, FilterCriteria]` | `[e.g., AggregateDetailsDto]` or `PaginatedList<SummaryDto>` | `[e.g., SELECT ... FROM "Table" WHERE "Id" = @Id]` |
| `GetRestaurantMenuQuery` | `Customer` | `RestaurantId` | `RestaurantMenuDto` (with categories and items) | `JOIN "MenuCategories" and "MenuItems" on "RestaurantId"` |
| `GetUserDetailsQuery` | `Admin`, `User (self)` | `UserId` | `UserDetailsDto` (with addresses, etc.) | `LEFT JOIN "Addresses" on "UserId"` |
| `SearchRestaurantsQuery`| `Customer` | `SearchTerm`, `Location`, `CuisineFilter`| `PaginatedList<RestaurantSearchResultDto>` | `Full-Text Search on "Restaurants" table, spatial query for location.` |

---

### 4. Domain Event Handling

***Instructions:*** *Identify the domain events this aggregate raises. For each event, list the decoupled side effects (handlers) that should run. This is key for building a loosely coupled system.*

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| **`[Aggregate][PastTenseVerb]Event`** | `[e.g., CreateAggregateCommand]` | `[e.g., NotifyUserOn... ]` | `[e.g., Sends an email or push notification.]` |
| `RestaurantVerified` | `VerifyRestaurantCommand` | `NotifyOwnerOnRestaurantVerified` | Sends an email to the restaurant owner informing them their profile is now live. |
| `RestaurantVerified` | `VerifyRestaurantCommand` | `IndexRestaurantForSearch` | Adds/updates the restaurant's data in the search engine (e.g., Elasticsearch). |
| `OrderPaid` | `(Internal payment process)` | `RecordRevenueForRestaurant` | Finds the `RestaurantAccount` aggregate, calls its `RecordRevenue()` method, and saves it. |

---

### 5. Key Business Logic & Application Service Orchestration

***Instructions:*** *For the most complex command(s), outline the step-by-step logic inside the command handler. This clarifies the orchestration of repository calls, domain service interactions, and aggregate method invocations.*

#### **`[ComplexCommandName]CommandHandler` Orchestration:**

1.  **Validate** the command's input using FluentValidation.
2.  **Authorize** the request (check roles, policies, and ownership).
3.  **Start a transaction** using `IUnitOfWork.ExecuteInTransactionAsync`.
4.  **Fetch required aggregates/entities:**
    *   `var aggregateToUpdate = await _repository.GetByIdAsync(...)`
    *   `var relatedEntity = await _otherRepository.GetByIdAsync(...)` (Fetch any other data needed for validation).
5.  **Perform pre-invocation business checks in the handler:**
    *   *Example:* Check if a related entity is in a valid state (e.g., `if (!restaurant.IsVerified) return Failure(...)`).
    *   *Example:* Check a cross-aggregate rule using a read model (e.g., `if (_couponUsageLookup.HasBeenUsed(couponId, userId)) return Failure(...)`).
6.  **(Optional) Use a Domain Service for complex calculations:**
    *   `var calculatedValue = _pricingService.Calculate(...)`
7.  **Invoke the Aggregate's Method:**
    *   `var result = aggregateToUpdate.PerformAction(parameter1, calculatedValue);`
    *   `if (result.IsFailure) return result;`
8.  **Persist the aggregate:**
    *   `await _repository.UpdateAsync(aggregateToUpdate);` (Or `AddAsync` for new aggregates).
9.  **Complete the transaction.** The `UnitOfWork` will commit, and `MediatR` will publish any domain events raised by the aggregate.
10. **Map and return** the response DTO.

---

### How to Use This Template

1.  **Copy-Paste:** Start a new document or section for each aggregate you plan to implement.
2.  **Top-Down Approach:** Begin with **Use Cases & Actors**. This defines *why* you're building the feature.
3.  **Define the Interface:** Fill out the **Commands** and **Queries** sections.
4.  **Connect the Dots:** Use the **Domain Event Handling** section to plan for side effects and communication between different parts of the system.
5.  **Detail the "How":** For any non-trivial command, write out the **Orchestration** steps in detail.
6.  **Think hard and practical:** Consider the real-world implications of the features you're designing.
