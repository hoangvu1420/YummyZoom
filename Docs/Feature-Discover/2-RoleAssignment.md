## Feature Discovery & Application Layer Design

### Aggregate Under Design: `RoleAssignment`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Restaurant Owner` | Invite a new staff member to their restaurant. | Creates a new `RoleAssignment` linking an existing `User` to their `Restaurant` with the `Staff` role. |
| `Restaurant Owner` | Revoke a staff member's access. | Marks an existing `RoleAssignment` as deleted, effectively removing the user's permissions for that restaurant. |
| `Restaurant Owner` | Promote/demote a staff member. | Updates an existing `RoleAssignment` to a different role (e.g., from `Staff` to `Owner`). *Note: This is a high-privilege action.* |
| `Admin` | Assign the initial owner of a restaurant. | Creates the first `RoleAssignment` for a restaurant, assigning the `Owner` role to the user who registered it. |
| `Admin` | Fix or manage restaurant staff. | As a support function, an Admin can create, update, or revoke any `RoleAssignment` on behalf of a restaurant owner. |
| `User (Self)` | View their assigned roles. | A user needs to see which restaurants they have access to and what their role is in each. |
| `System (Authorization)` | Check a user's permissions. | Before allowing an action (e.g., updating a menu), the system must query if a valid `RoleAssignment` exists for the user and restaurant. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization Policy |
| :--- | :--- | :--- | :--- | :--- |
| **`AssignRoleCommand`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `UserEmail`, `RestaurantRole` | `AssignRoleResponse(RoleAssignmentId)` | Must be `Owner` of the specified `RestaurantId` or an `Admin`. |
| **`UpdateRoleAssignmentCommand`**| `Restaurant Owner`, `Admin` | `RoleAssignmentId`, `NewRestaurantRole` | `Result.Success()` | Must be `Owner` of the associated restaurant or an `Admin`. |
| **`RevokeRoleAssignmentCommand`**| `Restaurant Owner`, `Admin` | `RoleAssignmentId` | `Result.Success()` | Must be `Owner` of the associated restaurant or an `Admin`. Cannot revoke your own `Owner` role if you are the last owner. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetStaffForRestaurantQuery`** | `Restaurant Owner`, `Admin` | `RestaurantId`, `PaginationParameters` | `PaginatedList<StaffMemberDto>` | `SELECT ra.*, u.Name, u.Email FROM "RoleAssignments" ra JOIN "Users" u ON ra.UserId = u.Id WHERE ra.RestaurantId = @RestaurantId` |
| **`GetUserRoleAssignmentsQuery`** | `User (Self)` | `UserId` | `List<UserRoleDto>` | `SELECT ra.*, r.Name, r.LogoUrl FROM "RoleAssignments" ra JOIN "Restaurants" r ON ra.RestaurantId = r.Id WHERE ra.UserId = @UserId` |
| **`CheckUserPermissionQuery`** | `System (Authorization)` | `UserId`, `RestaurantId` | `UserPermissionDto(RoleAssignmentId, Role)` or `null` | `SELECT Id, Role FROM "RoleAssignments" WHERE UserId = @UserId AND RestaurantId = @RestaurantId LIMIT 1` |

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| **`RoleAssignmentCreated`** | `AssignRoleCommand` | `NotifyUserOnRoleAssigned` | Sends an email notification to the user informing them they've been granted a new role for a specific restaurant. |
| **`RoleAssignmentCreated`** | `AssignRoleCommand` | `LogAdminActionOnRoleAssignment` | If the action was performed by an Admin, creates an audit log entry detailing the change. |
| **`RoleAssignmentDeleted`** | `RevokeRoleAssignmentCommand` | `NotifyUserOnRoleRevoked` | Sends an email notification to the user informing them their access to a restaurant has been revoked. |
| **`RoleAssignmentDeleted`** | `RevokeRoleAssignmentCommand` | `InvalidateUserSessionCache` | Clears any cached authorization claims for the affected user, forcing a permission re-evaluation on their next action to ensure access is immediately revoked. |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`AssignRoleCommandCommandHandler` Orchestration:**

1.  **Validate** the command's input: `RestaurantId`, `UserEmail`, and `Role` must not be null/empty. `UserEmail` must be a valid email format.
2.  **Authorize** the request:
    *   Retrieve the `UserId` of the current logged-in user (the *invoker*).
    *   Execute `CheckUserPermissionQuery` with the invoker's `UserId` and the command's `RestaurantId`.
    *   Confirm the invoker's role is `Owner` (or that the invoker is an `Admin`). If not, return `Forbidden` error.
3.  **Start a transaction** using `IUnitOfWork`.
4.  **Fetch required entities:**
    *   Find the user to be assigned: `var userToAssign = await _userRepository.FindByEmailAsync(command.UserEmail)`.
    *   If `userToAssign` is null, return a `UserNotFound` error.
5.  **Perform pre-invocation business checks in the handler:**
    *   Check for uniqueness to enforce the domain invariant. Execute a query: `var existingAssignment = await _roleAssignmentRepository.FindByUserAndRestaurantAsync(userToAssign.Id, command.RestaurantId)`.
    *   If `existingAssignment` is not null, return a `RoleAssignmentAlreadyExists` error.
6.  **(Optional) Domain Service:** Not required for this command.
7.  **Invoke the Aggregate's Method:**
    *   Call the static factory: `var creationResult = RoleAssignment.Create(userToAssign.Id, command.RestaurantId, command.Role);`
    *   If `creationResult.IsFailure`, return the failure result (e.g., `RoleAssignmentErrors.InvalidRole`).
8.  **Persist the new aggregate:**
    *   `await _roleAssignmentRepository.AddAsync(creationResult.Value);`
9.  **Complete the transaction.** The `UnitOfWork` commits changes and dispatches the `RoleAssignmentCreated` domain event.
10. **Map and return** the `AssignRoleResponse` DTO containing the new `RoleAssignmentId`.

---

### Design Notes & Suggestions

1.  **Contradiction in Invariants:** There is a conflict between the provided documents.
    *   `Domain_Design.md`: States the uniqueness invariant is on `(UserID, RestaurantID)`. This implies a user can have only one role per restaurant.
    *   `2-RoleAssignment-Aggregate.md`: States the uniqueness invariant is on `(UserID, RestaurantID, Role)`. This implies a user could have both an `Owner` and a `Staff` role for the same restaurant, which is illogical.
    *   **Recommendation:** The design above assumes the `Domain_Design.md` is the source of truth (`UserID + RestaurantID` must be unique). This is a more robust and common business rule. The `RoleAssignment` aggregate documentation should be updated to reflect this.

2.  **Last Owner Check:** The business logic for `RevokeRoleAssignmentCommand` must include a critical check: **a restaurant must always have at least one owner**. The command handler should query how many users have the `Owner` role for the given restaurant. If the user being revoked is the last owner, the command must fail. This logic belongs in the Application Service, as it requires querying data outside the single aggregate instance being modified.

3.  **Event Sourcing Consideration:** The `RoleAssignmentCreated` and `RoleAssignmentDeleted` events are perfect candidates for creating an audit trail. An event handler `CreateRoleAssignmentAuditLog` could listen to these events and write immutable records to an `AuditLogs` table, capturing who made the change, to whom, and when. This provides a more robust history than just relying on the `UpdatedAt` timestamp.