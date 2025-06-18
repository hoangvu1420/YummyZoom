**Current State:**

* The `User` domain aggregate exists and includes a collection of `RoleAssignment` value objects.
* The `RoleAssignment` value object correctly models the role with `RoleName`, `TargetEntityId`, and `TargetEntityType`.
* The `IdentityService` handles user registration and assigns a default "Customer" role in both the Identity system and the domain model within a transaction.

**Implementation Plan:**

1. **Enhance Domain `User` Aggregate:**
    * Review and refine the `AddRole` and `RemoveRole` methods in `src/Domain/UserAggregate/User.cs` to ensure they handle invariants correctly (e.g., preventing removal of the last role). These methods currently exist but may need minor adjustments based on specific business rules.
    * Consider adding domain events like `RoleAssignmentAddedToUserEvent` and `RoleAssignmentRemovedFromUserEvent` within the `AddRole` and `RemoveRole` methods. These events will be crucial for triggering the synchronization with the Identity system.

2. **Implement Application Services for Role Management:**
    * Create new commands and command handlers in the `Application` layer (e.g., `AssignRoleToUserCommand`, `RemoveRoleFromUserCommand`).
    * These command handlers will:
        * Load the `User` aggregate using the `IUserAggregateRepository`.
        * Call the appropriate `AddRole` or `RemoveRole` method on the `User` aggregate.
        * Save the updated `User` aggregate using the repository.
        * **Crucially, after saving the aggregate and dispatching domain events (this is typically handled by the MediatR pipeline and a `SaveChanges` behavior), react to the `RoleAssignmentAddedToUserEvent` and `RoleAssignmentRemovedFromUserEvent` domain events.**

3. **Implement Domain Event Handlers for Identity Synchronization:**
    * Create new domain event handlers in the `Application` layer (e.g., `RoleAssignmentAddedToUserEventHandler`, `RoleAssignmentRemovedFromUserEventHandler`).
    * These handlers will be triggered by the domain events raised in the `User` aggregate.
    * Inside these handlers:
        * Retrieve the corresponding `ApplicationUser` from the Identity system using `UserManager`.
        * Translate the domain `RoleAssignment` into the appropriate Identity role string(s). This translation logic will need to be defined. For example, a `RoleAssignment` with `RoleName="RestaurantOwner"` and `TargetEntityId="restaurant-xyz"` might translate to the Identity role `"RestaurantOwner"`. More complex scenarios might require generating roles like `"Owner_restaurant-xyz"`, though claims are generally preferred for entity-specific permissions.
        * Call `_userManager.AddToRoleAsync()` or `_userManager.RemoveFromRoleAsync()` to synchronize the Identity roles.
        * Ensure these operations are part of the same transaction as saving the domain aggregate. This might require adjustments to the transaction handling in the application service or using a transactional outbox pattern if cross-concerns become complex. The current `IdentityService.CreateUserAsync` shows a pattern of using `DbContext.Database.CreateExecutionStrategy().ExecuteAsync` with a transaction, which could be adapted.

4. **Refine IdentityService:**
    * Add new methods to `IdentityService` (or modify existing ones) to encapsulate the logic for adding/removing Identity roles based on a `UserId` and the translated Identity role string. This will keep the domain event handlers focused on the translation and calling the Identity layer.

5. **Authorization Implementation:**
    * For coarse-grained checks (`[Authorize(Roles = "Admin")]`), rely on the Identity roles which are now synchronized projections of the domain roles.
    * For fine-grained checks (e.g., "Is this user the owner of *this specific* restaurant?"), continue to load the domain `User` aggregate and inspect its `RoleAssignments` collection directly within the application service or a dedicated domain service.

**Transaction Management:**

The synchronization between the domain and Identity system must be atomic. The current `IdentityService.CreateUserAsync` uses a transaction spanning both Identity user creation and domain user creation. This pattern should be extended to the role assignment/removal process. The application service or domain event handler that initiates the change should ensure that the Identity role update happens within the same transaction as saving the domain `User` aggregate.

**Next Steps:**

I will start by implementing the domain event handlers and the necessary methods in `IdentityService` to handle the synchronization. I will also create the application services (commands and handlers) for assigning and removing roles.

Does this plan align with your expectations? Are there any specific roles or translation rules you'd like to define upfront?
