# Aggregate Documentation: `RoleAssignment`

* **Version:** 1.0
* **Last Updated:** 2025-07-05
* **Source File:** `e:\source\repos\CA\YummyZoom\src\Domain\RoleAssignmentAggregate\RoleAssignment.cs`

## 1. Overview

**Description:**
*A dedicated aggregate that explicitly links a User to a Restaurant with a specific role. This is the authoritative source for determining a user's permissions and responsibilities within the context of a restaurant.*

**Core Responsibilities:**

* Manages the lifecycle of a role assignment.
* Acts as the transactional boundary for all role assignment operations.
* Enforces the business rule that the combination of `UserID`, `RestaurantID` must be unique.

## 2. Structure

* **Aggregate Root:** `RoleAssignment`
* **Key Child Entities:** None
* **Key Value Objects:**
  * `RoleAssignmentId`: The unique identifier for the `RoleAssignment` aggregate.
  * `UserId`: A reference to the `User` aggregate.
  * `RestaurantId`: A reference to the `Restaurant` aggregate.

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `RoleAssignment` is through its static factory method.

```csharp
public static Result<RoleAssignment> Create(
  UserId userId,
  RestaurantId restaurantId,
  RestaurantRole role
)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `userId` | `UserId` | The ID of the user to assign the role to. |
| `restaurantId` | `RestaurantId` | The ID of the restaurant. |
| `role` | `RestaurantRole` | The role to assign. |

**Validation Rules & Potential Errors:**

* `userId` cannot be null. (Returns `RoleAssignmentErrors.InvalidUserId`)
* `restaurantId` cannot be null. (Returns `RoleAssignmentErrors.InvalidRestaurantId`)
* `role` must be a defined `RestaurantRole`. (Returns `RoleAssignmentErrors.InvalidRole`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result UpdateRole(RestaurantRole newRole)` | Updates the role for this assignment. | Checks if `newRole` is a valid `RestaurantRole`. | `RoleAssignmentErrors.InvalidRole` |
| `Result MarkAsDeleted()` | Marks this role assignment for deletion. | None. | None. |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `RoleAssignmentId` | The unique identifier of the aggregate. |
| `UserId` | `UserId` | The ID of the assigned user. |
| `RestaurantId` | `RestaurantId` | The ID of the restaurant. |
| `Role` | `RestaurantRole` | The assigned role. |

### 4.2. Public Query Methods

| Method Signature | Description |
| :--- | :--- |
| `bool Matches(UserId userId, RestaurantId restaurantId, RestaurantRole role)` | Returns `true` if the assignment matches the given criteria. |
| `bool IsForUserAndRestaurant(UserId userId, RestaurantId restaurantId)` | Returns `true` if the assignment is for the given user and restaurant. |

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `RoleAssignmentCreated` | During the `Create` factory method. | Signals that a new role assignment has been created. |
| `RoleAssignmentUpdated` | During `UpdateRole` when the role actually changes. | Signals that an existing role assignment has been updated with a new role. Contains both the previous and new roles. |
| `RoleAssignmentDeleted` | During `MarkAsDeleted`. | Signals that a role assignment has been marked for deletion. |
