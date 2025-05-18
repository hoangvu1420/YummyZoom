# Proper Way to Work with the Two-Identity Model (Ensuring Consistency & Integrity)

**Guiding Principles:**

1. **Single Source of Truth (SSoT) where possible, or clear synchronization strategy.**
2. **Orchestration Layer:** Use Application Services to coordinate operations across Identity and Domain.
3. **Domain Events for Decoupling (Optional but Recommended):** For more complex synchronization.
4. **Keep Identity Layer Focused:** The ASP.NET Core Identity layer should primarily handle authentication and basic user account management (password resets, lockout, etc.).

**Recommended Approach:**

**A. User ID Management:**

* **SSoT for ID:** The `ApplicationUser.Id` (from ASP.NET Core Identity) **should be the single source of truth for the user's unique identifier.**
* **Domain User ID:** The `Domain.User.Id` (your `UserId` strongly-typed ID) **must always be created from and match** the `ApplicationUser.Id`.
  * Your `UserId.Create(Guid value)` is perfect for this.
* **Creation:** Your current approach in `CreateUserAsync` where `domainUserIdResult = UserId.Create(identityUser.Id)` is correct.

**B. Profile Data (Email, Phone Number, Name):**

* **Option 1: Identity as SSoT for Core Authentication Attributes (Recommended for Email):**
  * **Email:** Treat `ApplicationUser.Email` (and `ApplicationUser.UserName`, which you're setting to email) as the SSoT for the login identifier. The domain `User.Email` is a synchronized copy.
  * **Phone Number:** Can also be primarily managed by Identity if used for 2FA, etc. Domain `User.PhoneNumber` is a synchronized copy.
  * **Name:** This is more of a domain concept. The domain `User.Name` can be the SSoT.
  * **Synchronization Strategy:**
    * **On Creation:** Populate both as you do.
    * **On Update (e.g., User changes email via a profile page):**
        1. The Application Service receives the update request.
        2. It calls `UserManager.SetEmailAsync()` and `UserManager.SetUserNameAsync()`. This is crucial because Identity has its own validation and normalization for these.
        3. **If successful,** the Application Service then fetches the domain `User` aggregate (using the `ApplicationUser.Id`) and calls a method like `userAggregate.UpdateEmail(newEmail)`.
        4. All operations should be within a transaction.
    * **Alternative (using Domain Events):** When `UserManager.SetEmailAsync` is successful, `IdentityService` could raise a domain event (e.g., `IdentityUserEmailChangedEvent`). A handler for this event would then update the domain `User` aggregate. This decouples `IdentityService` from the domain update logic.

* **Option 2: Domain as SSoT for Profile Data (More Complex with Identity):**
  * Domain `User` holds the master copy.
  * When domain `User` is updated, you must programmatically update the `ApplicationUser` via `UserManager`. This can be tricky if Identity has specific validation/normalization you need to bypass or replicate.
  * Generally, for attributes directly used by Identity for its core functions (like email for login, phone for 2FA), letting Identity manage them and synchronizing to the domain is safer.

**C. Role Management:**

The goal is for a consistent view of a user's roles/permissions, regardless of whether Identity or your domain authorization checks are performed.

* **Strategy: Domain `RoleAssignment` as SSoT for effective permissions; Identity Roles as a projection/simplification for basic ASP.NET Core authorization.**

* **1. Defining Roles:**
  * **Domain-First:** Define all conceptual roles and their meanings (including `TargetEntityId` linkage) within your domain `User.RoleAssignments`.
  * **Identity Roles:** ASP.NET Core Identity roles (`string` names like "Admin", "Customer", "RestaurantOwner_restaurant-123") will be *derived* from the domain `RoleAssignments`.

* **2. Synchronization Logic (Bi-directional or Orchestrated):**
  * **Adding a Domain `RoleAssignment`:**
        1. An Application Service calls `userAggregate.AddRole(newRoleAssignment)`.
        2. After successfully saving the `User` aggregate:
            *The Application Service (or a domain event handler reacting to `RoleAddedToUserEvent`) translates this `RoleAssignment` into one or more ASP.NET Core Identity role strings.
                * If `newRoleAssignment` is `RoleName="RestaurantOwner", TargetEntityId="restaurant-xyz"`:
                    *You might add the user to an Identity role like `"RestaurantOwner"` (generic).
                    * And/or potentially a specific Identity role like `"Owner_restaurant-xyz"` if you need very granular Identity role checks (less common, claims are better for this).
            * Call `UserManager.AddToRoleAsync()` or `UserManager.AddToRolesAsync()`.
        3. All within a transaction.

  * **Removing a Domain `RoleAssignment`:**
        1. Application Service calls `userAggregate.RemoveRole(...)`.
        2. After successfully saving the `User` aggregate:
            * Translate and call `UserManager.RemoveFromRoleAsync()`.
        3. All within a transaction.

  * **Changes via Identity Roles (Discouraged if possible, but for completeness):**
    * If an admin directly uses an Identity UI to add a user to an Identity role like "SuperAdmin":
      * This is hard to sync back to the structured domain `RoleAssignment` without more context (e.g., is "SuperAdmin" global or for a specific entity?).
      * **Recommendation:** Try to make all *authoritative* role changes happen through your application services that manipulate the domain `User` aggregate first. The Identity roles become a read-only or managed projection.

* **3. Authorization Checks:**
  * **ASP.NET Core `[Authorize(Roles = "Admin")]`:** This will check against the Identity roles. These should be sufficient for coarse-grained authorization.
  * **Fine-grained Domain Authorization:** For checks like "Is this user the owner of *this specific* restaurant?", you will:
        1. Load the domain `User` aggregate.
        2. Inspect its `userAggregate.RoleAssignments` list.
        3. Example: `user.RoleAssignments.Any(ra => ra.RoleName == "RestaurantOwner" && ra.TargetEntityId == specificRestaurantId.Value.ToString())`.

**D. `IdentityService` Responsibilities:**

* **Focus on ASP.NET Core Identity Operations:** Creating `ApplicationUser`, password management, finding users by ID/email for Identity purposes.
* **Avoid Direct Domain Manipulation:** It should not directly call `_userAggregateRepository.AddAsync()` or know about domain aggregate creation logic.
* **Return `ApplicationUser.Id`:** Its `CreateUserAsync` should return the `ApplicationUser.Id` (the `Guid`).
* **Raise Events (Optional but Good):** After successfully creating an `ApplicationUser` (e.g., `IdentityUserAccountCreatedEvent(Guid userId, string email)`).

**E. Application Service Orchestration (Example: `UserRegistrationService`):**

```csharp
public class UserRegistrationService // In Application Layer
{
    private readonly IIdentityService _identityService; // Your existing one, but potentially slimmed down
    private readonly IUserAggregateRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork; // For managing transactions across DB operations

    public UserRegistrationService(IIdentityService identityService, IUserAggregateRepository userRepository, IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> RegisterUserAsync(string email, string password, string firstName, string lastName)
    {
        // 1. Attempt to create the Identity user
        // IdentityService.CreateUserAsync should now ONLY create the ApplicationUser
        // and NOT the domain user. It returns the ApplicationUser.Id (Guid).
        var identityUserIdResult = await _identityService.CreateIdentityAccountAsync(email, password);
        if (identityUserIdResult.IsFailure)
        {
            return Result.Failure<Guid>(identityUserIdResult.Error);
        }
        Guid newUserId = identityUserIdResult.Value;

        // 2. Create the Domain User
        var domainUserId = UserId.Create(newUserId); // Use the ID from Identity
        var fullName = $"{firstName} {lastName}".Trim();

        var initialRoleResult = RoleAssignment.Create("Customer"); // Default role
        if (initialRoleResult.IsFailure)
        {
            // This is an internal error, should be logged.
            // Might need to consider cleaning up the Identity user if this fails critically.
            return Result.Failure<Guid>(initialRoleResult.Error);
        }

        var userAggregateResult = User.Create(
            fullName,
            email,
            null, // phoneNumber
            new List<RoleAssignment> { initialRoleResult.Value }
        );

        // IMPORTANT: The User.Create method in your domain should use the passed-in ID
        // if you want to link it, or you ensure the User.Id matches the identityUser.Id.
        // Modify User.Create to accept an optional UserId or ensure it uses the provided ID.
        // For example, if User.Create internally calls UserId.CreateUnique(), that's a mismatch.
        // It should be:
        // var userAggregateResult = User.Create(
        //     domainUserId, // Pass the ID explicitly
        //     fullName, email, null, new List<RoleAssignment> { initialRoleResult.Value });

        if (userAggregateResult.IsFailure)
        {
            // TODO: Consider deleting the newly created IdentityUser if domain creation fails.
            // This is where compensating transactions become important.
            // For now, returning failure.
            return Result.Failure<Guid>(userAggregateResult.Error);
        }
        var userAggregate = userAggregateResult.Value;

        // Ensure the domain user's ID matches the identity user's ID.
        // This check is crucial if User.Create might generate its own ID.
        // Ideally, User.Create should take the UserId as a parameter.
        if (userAggregate.Id.Value != newUserId)
        {
           // Critical mismatch, indicates an issue in User.Create or ID propagation.
           // TODO: Handle this error, potentially delete IdentityUser.
           return Result.Failure<Guid>(Error.Unexpected("ID mismatch between Identity and Domain user."));
        }

        await _userRepository.AddAsync(userAggregate);

        // 3. Save changes (transaction managed by UnitOfWork or DbContext.SaveChanges if simple)
        try
        {
            await _unitOfWork.SaveChangesAsync(CancellationToken.None); // Or dbContext.SaveChangesAsync()
            return Result.Success(newUserId); // Return the consistent ID
        }
        catch (DbUpdateException ex) // Catch potential DB errors like unique constraint violations
        {
            // TODO: Check if it's a duplicate email for the domain user (if you have a unique index there too)
            // TODO: Consider deleting the IdentityUser as a compensating action.
            return Result.Failure<Guid>(UserErrors.RegistrationFailed($"Database error: {ex.InnerException?.Message ?? ex.Message}"));
        }
    }
}
```

**Revised `User.Create` in Domain:**

Your `User.Create` method in `User.cs` currently does `UserId.CreateUnique()`. This **must change** if you want the domain `User.Id` to match the `ApplicationUser.Id`.

```csharp
// In src\Domain\UserAggregate\User.cs
public static Result<User> Create(
    UserId id, // <<<< ADD THIS PARAMETER
    string name,
    string email,
    string? phoneNumber,
    List<RoleAssignment> userRoles)
{
    if (userRoles == null || userRoles.Count == 0)
    {
        return Result.Failure<User>(UserErrors.MustHaveAtLeastOneRole);
    }

    // Use the provided ID
    var user = new User(
        id, // <<<< USE THE PASSED ID
        name,
        email,
        phoneNumber,
        userRoles,
        new List<Address>(), // Initialize empty lists
        new List<PaymentMethod>() // Initialize empty lists
    );

    // user.AddDomainEvent(new UserCreated(user)); // UserCreated event should include the ID
    return Result.Success(user);
}
```

Then, in your `UserRegistrationService` (or the refactored `IdentityService`):

```csharp
// ...
UserId domainUserId = UserId.Create(newUserId); // newUserId is Guid from ApplicationUser
// ...
var userAggregateResult = User.Create(
    domainUserId, // Pass the created domainUserId
    fullName,
    email,
    null,
    new List<RoleAssignment> { initialRoleAssignment.Value }
);
// ...
```

**F. Transactions:**

* The `IUnitOfWork` pattern (or just ensuring `_dbContext.SaveChangesAsync()` is called once after all modifications to both Identity and Domain entities) is critical for atomicity. If `ApplicationDbContext` is used for both Identity (`IdentityDbContext`) and your domain entities, a single `SaveChangesAsync` call can often commit changes to both within the same underlying database transaction. Your current `BeginTransactionAsync` in `IdentityService` is a good step in this direction.

**Summary of Key Actions:**

1. **Refactor `IdentityService`:** Make it focus solely on `ApplicationUser` lifecycle. It should create the `ApplicationUser` and return its `Guid` ID.
2. **Create an Orchestrating Application Service** (e.g., `UserRegistrationService`):
    * Calls `IdentityService` to create the `ApplicationUser`.
    * Uses the returned `Guid` to create a `UserId`.
    * Calls `Domain.User.Create(userId, ...)` (passing the `UserId`) to create the domain aggregate.
    * Adds the domain user to its repository.
    * Orchestrates role synchronization between domain `RoleAssignment` and Identity roles.
    * Manages the overall transaction (`IUnitOfWork.SaveChangesAsync()` or `DbContext.SaveChangesAsync()`).
3. **Modify `Domain.User.Create`:** It *must* accept a `UserId` parameter to ensure the domain user's ID matches the Identity user's ID.
4. **Profile Sync Strategy:** Decide SSoT for email/phone. If Identity is SSoT, updates go Identity -> Domain.
5. **Role Sync Strategy:** Domain `RoleAssignments` are primary. Changes there are projected to Identity roles. Avoid direct manipulation of Identity roles if possible; if not, build a sync mechanism.

This two-identity model is powerful but requires careful orchestration to maintain consistency. Prioritize clear data flow and responsibility boundaries.
