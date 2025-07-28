Excellent question. This is one of the most critical architectural decisions in a system with interconnected data, and getting it wrong can lead to data corruption, broken features, and legal issues.

Here is a detailed breakdown of how to handle deletions, starting with the `User` aggregate and then generalizing the principles.

### The Core Problem: Referential Integrity vs. Historical Accuracy

When a `User` is referenced by an `Order`, a `Review`, and a `RoleAssignment`, a simple `DELETE FROM Users WHERE Id = @UserId` would cause chaos:

*   **Orphaned Records:** The `Order` and `Review` tables would now have a `CustomerID` that points to nothing. This is known as a dangling reference.
*   **Application Errors:** Any code that tries to look up the user for an old order (e.g., to display "Order placed by John Doe") will fail.
*   **Loss of Business Intelligence:** How do you calculate the total value of orders placed last year if the records of who placed them are gone? The historical and financial integrity of your system is compromised.

### The Solution: Strategy Depends on the Data's Purpose

You never use a single deletion strategy for all data. The right choice depends on the aggregate's role.

---

### Deleting a `User`: The Two-Phase "Soft Delete + Anonymization"

For a core entity like a `User`, which is linked to vital historical records, you must use a sophisticated, multi-step process. **You should never hard-delete a user directly.**

#### Phase 1: Immediate Action (Soft Delete)

This is what happens the moment a user or an admin clicks "Delete Account".

1.  **The Command:** A `DeleteUserAccountCommand` is dispatched.
2.  **The Action:** The `User` aggregate's `MarkAsDeleted()` method is called.
3.  **The State Change:** The user's record in the `Users` table is updated:
    *   `IsDeleted = true`
    *   `DeletedAt = '2025-10-26 10:00:00'`
4.  **Immediate Effect:**
    *   The user is immediately logged out.
    *   All authentication queries will now fail for this user because of the `IsDeleted` flag.
    *   From the user's perspective, their account is gone. This action is fast, simple, and reversible by an admin if needed.

#### Phase 2: Background Process (Anonymization & Cleanup)

This is the crucial step for long-term data management and GDPR compliance ("right to be forgotten"). It's triggered by the `UserDeleted` domain event raised in Phase 1. This process **must run in the background** (e.g., using a message queue and a background worker) because it can be slow and complex.

A **Saga or Process Manager** is the perfect pattern to orchestrate this:

1.  **Trigger:** The `UserDeleted` event is received by the `AnonymizeUserDataSaga`.
2.  **Step 1: Revoke Access Rights.** The saga finds all `RoleAssignment` records for the deleted `UserId` and **hard-deletes** them. Access rights are not historical business facts; they are ephemeral permissions that should be completely removed.
3.  **Step 2: Anonymize Financial/Historical Records.** The saga finds all aggregates that must preserve history but remove Personally Identifiable Information (PII).
    *   **For `Order`s:** It finds all orders where `CustomerID == deletedUserId` and updates them:
        *   `Order.CustomerID` -> `NULL` or a predefined `System_Anonymous_UserID`.
        *   `Order.DeliveryAddress` -> The address snapshot is scrubbed (e.g., street, city set to "N/A").
        *   The order record itself, with its items and total amount, **is preserved**.
    *   **For `Review`s:** It finds all reviews by the user and updates:
        *   `Review.CustomerID` -> `NULL` or `System_Anonymous_UserID`.
        *   The `Comment` might be kept, but the association is broken.
4.  **Step 3: (Optional) Final Hard Delete.** After the saga confirms that all references in other aggregates have been successfully anonymized, it can perform the final step: **a hard delete of the record from the `Users` table**. This achieves full data removal and allows the user's email to be used for a new registration in the future.

This two-phase approach gives you the best of all worlds: immediate user feedback, preservation of historical data integrity, and eventual compliance with data privacy regulations.

---

### Generalizing the Deletion Strategy for Other Aggregates

Here is a table summarizing the recommended strategy for different aggregates in your system:

| Aggregate / Entity | Deletion Strategy | Rationale |
| :--- | :--- | :--- |
| **`User`** | **Soft Delete + Anonymization** | As described above. It's a central identity linked to critical history. PII must be scrubbed, but the history of its actions must be preserved in an anonymous form. |
| **`Order`** | **Immutable (No Deletion)** | An order is a **financial and legal record**. It should never be deleted. It can transition to a `Cancelled` or `Refunded` state, but the record must persist for accounting, auditing, and reporting. |
| **`Restaurant`** | **Soft Delete** | A restaurant is tied to orders, menus, and financial data (`RestaurantAccount`). A hard delete would orphan all that data. It should be "deactivated" or "archived" (`IsActive = false`), removing it from public view but keeping its data for historical analysis. |
| **`MenuItem`** | **Soft Delete** | A restaurant owner might want to temporarily disable an item (e.g., seasonal) and bring it back later. A hard delete would be destructive. A simple `IsAvailable = false` or `IsArchived = true` flag is perfect. |
| **`CustomizationGroup`**| **Soft Delete** | Same as `MenuItem`. An owner might want to reuse a group of options later. Deleting it should just hide it from being assigned to *new* menu items. A background process should then disassociate it from existing items. |
| **`RoleAssignment`** | **Hard Delete** | This represents a permission, not a historical fact. When access is revoked, the record should be completely and permanently removed. There is no business value in keeping a "deleted" role assignment. |
| **`Review`** | **Soft Delete** | A review should be hidden (`IsHidden = true`) by a moderator, not hard-deleted. This allows for a moderation trail and the ability to restore the review if it was hidden by mistake. The user who wrote it should not be able to delete it to prevent gaming the rating system. |
| **`AccountTransaction`**| **Immutable (No Deletion)** | Like an `Order`, this is a financial ledger entry. It is an immutable fact that cannot be deleted. Corrections are made by adding new, opposing transactions (e.g., a `RefundDeduction` to counter an `OrderRevenue`). |

### Summary of Principles

1.  **If it's a financial or legal record, make it IMMUTABLE.** (e.g., `Order`, `AccountTransaction`).
2.  **If it's a central identity linked to history, use SOFT DELETE + ANONYMIZATION.** (e.g., `User`).
3.  **If it's operational data that a business user might want to reuse or restore, use SOFT DELETE.** (e.g., `Restaurant`, `MenuItem`, `Review`).
4.  **If it's an ephemeral permission or a link with no historical value, use HARD DELETE.** (e.g., `RoleAssignment`).

---
---

---

### The Proposed New Pattern: Composition via Interfaces

Instead of forcing all capabilities into the base `Entity` class, we will define a suite of small, single-responsibility interfaces. Aggregates and Entities will then implement only the interfaces they need.

#### 1. New & Refactored Interfaces

We will break down the capabilities into the following interfaces:

**`ISoftDeletableEntity.cs` (New)**
This interface provides a standard contract for entities that should be soft-deleted.

```csharp
namespace YummyZoom.Domain.Common.Models;

public interface ISoftDeletableEntity
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedOn { get; }
    // We could add DeletedBy if needed, but often the LastModifiedBy from IAuditableEntity is sufficient.

    // Note: The method to perform the deletion will be on the concrete class,
    // as the interface just exposes the state.
}
```

**`ICreationAuditable.cs` (New)**
For immutable entities that only need to record their creation time.

```csharp
namespace YummyZoom.Domain.Common.Models;

public interface ICreationAuditable
{
    DateTimeOffset Created { get; set; }
    string? CreatedBy { get; set; }
}
```

**`IModificationAuditable.cs` (New)**
For entities that track modification time.

```csharp
namespace YummyZoom.Domain.Common.Models;

public interface IModificationAuditable
{
    DateTimeOffset LastModified { get; set; }
    string? LastModifiedBy { get; set; }
}
```

**`IAuditableEntity.cs` (Modified)**
This now becomes a composite interface for convenience.

```csharp
namespace YummyZoom.Domain.Common.Models;

// A composite interface for entities that are fully auditable
public interface IAuditableEntity : ICreationAuditable, IModificationAuditable
{
}
```

---

#### 2. Refactored Base Classes

The base `Entity` class becomes much leaner.

**`Entity.cs` (Refactored)**
It is now only responsible for `Id` and `DomainEvents`.

```csharp
namespace YummyZoom.Domain.Common.Models;

public abstract class Entity<TId> : IEquatable<Entity<TId>>, IHasDomainEvent
    where TId : ValueObject
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public TId Id { get; protected set; }
    
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity(TId id)
    {
        Id = id;
    }
    
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Rest of the equality logic remains the same...
    
#pragma warning disable CS8618
    protected Entity()
    {
    }
#pragma warning restore CS8618
}
```

The `AggregateRoot` class requires no changes, as it just inherits from the new, leaner `Entity`.

---

#### 3. Example Aggregate Implementations

Now you can define your aggregates with exactly the capabilities they need.

**`User.cs` (Needs full audit and soft delete)**
```csharp
public class User : AggregateRoot<UserId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOn { get; private set; }

    // ... other user properties

    // The method now accepts the timestamp as a parameter.
    public Result MarkAsDeleted(DateTimeOffset deletedOn)
    {
        if (IsDeleted)
        {
            // Optionally, handle re-deleting an already deleted entity.
            return Result.Success(); 
        }

        IsDeleted = true;
        DeletedOn = deletedOn; // Explicitly set the timestamp here.
        
        AddDomainEvent(new UserDeleted(Id));
        return Result.Success();
    }
}
```

**`AccountTransaction.cs` (Immutable, needs creation audit only)**
```csharp
// Note: This is an Entity, not an Aggregate Root
public class AccountTransaction : Entity<AccountTransactionId>, ICreationAuditable
{
    // Properties from ICreationAuditable
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    // ... other immutable properties
    // NO LastModified, NO IsDeleted. This is semantically correct.
}
```

**`RoleAssignment.cs` (Hard-deleted, needs creation audit only)**
```csharp
public class RoleAssignment : AggregateRoot<RoleAssignmentId, Guid>, ICreationAuditable
{
    // Properties from ICreationAuditable
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    
    // ... other properties

    // NO MarkAsDeleted method. Deleting this will be a hard delete.
}
```

---

#### 4. Upgraded Infrastructure Layer (Interceptors & DbContext)

This is where the magic happens. We'll update the interceptors to be aware of the new interfaces and add a new interceptor for soft deletes.

**`AuditableEntityInterceptor.cs` (Modified)**
The interceptor now handles the new, granular interfaces correctly.

```csharp
// ... (usings)

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    // ... (constructor remains the same)

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var utcNow = _dateTime.GetUtcNow();
        var userId = _user.Id;

        foreach (var entry in context.ChangeTracker.Entries<ICreationAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = userId;
                entry.Entity.Created = utcNow;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<IModificationAuditable>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedBy = userId;
                entry.Entity.LastModified = utcNow;
            }
        }
    }
}
```

**`SoftDeleteInterceptor.cs` (New)**
This new interceptor handles the logic for soft-deleting entities.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null) return base.SavingChanges(eventData, result);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                // The IsDeleted and DeletedOn properties are set by the aggregate's MarkAsDeleted method.
                // This interceptor's only job is to prevent the physical deletion.
            }
        }

        return base.SavingChanges(eventData, result);
    }
    
    // Implement the async version as well
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

**`ApplicationDbContext.cs` (Modified)**
Finally, to make soft deletion seamless, we add a global query filter.

```csharp
public class ApplicationDbContext : DbContext
{
    // ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... other configurations

        // Apply global query filter for soft deletable entities
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Convert.ToLambda<Func<ISoftDeletableEntity, bool>>(
                        Expression.Not(Expression.Property(Expression.Parameter(entityType.ClrType), "IsDeleted")),
                        Expression.Parameter(entityType.ClrType)));
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
```
*Note: The reflection code for the query filter ensures you don't have to add it manually for every new soft-deletable entity.*

### Summary of Benefits

This new pattern provides:

1.  **Flexibility:** You choose the exact capabilities (auditing, soft-deletion) for each entity.
2.  **Semantic Correctness:** Immutable entities are no longer polluted with `LastModified` properties.
3.  **Separation of Concerns:** Deletion logic is handled by a dedicated `SoftDeleteInterceptor`, and auditing by the `AuditableEntityInterceptor`.
4.  **Robustness:** Global query filters prevent developers from accidentally fetching soft-deleted data.
5.  **Clarity:** The intent of each entity is clear from the interfaces it implements.

---

### Option 1: The Domain-Centric Approach (Recommended)

In this approach, the aggregate itself is responsible for setting its own state completely. This is the most pure DDD approach because the aggregate's methods are the sole source of truth for state changes.

#### How to Implement It:

1.  **Inject a Time Provider into the Method:** The timestamp should be passed into the method, not generated inside it, to make the domain model independent of system concerns and highly testable.

    **`User.cs` (Refined `MarkAsDeleted` method)**
    ```csharp
    public class User : AggregateRoot<UserId, Guid>, IAuditableEntity, ISoftDeletableEntity
    {
        // ... properties
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedOn { get; private set; }

        // The method now accepts the timestamp as a parameter.
        public Result MarkAsDeleted(DateTimeOffset deletedOn)
        {
            if (IsDeleted)
            {
                // Optionally, handle re-deleting an already deleted entity.
                return Result.Success(); 
            }

            IsDeleted = true;
            DeletedOn = deletedOn; // Explicitly set the timestamp here.
            
            AddDomainEvent(new UserDeleted(Id));
            return Result.Success();
        }
    }
    ```

2.  **The Application Service provides the time:** The command handler in the Application Layer is responsible for getting the current time and passing it to the domain method.

    **`DeleteUserAccountCommandHandler.cs`**
    ```csharp
    public class DeleteUserAccountCommandHandler : IRequestHandler<DeleteUserAccountCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider; // Inject the time provider here.

        public DeleteUserAccountCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task<Result> Handle(DeleteUserAccountCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(new UserId(request.UserId));
            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound);
            }

            // Get the current time and pass it to the domain method.
            var utcNow = _timeProvider.GetUtcNow();
            var result = user.MarkAsDeleted(utcNow);

            if(result.IsFailure) return result;

            // The repository will handle the update.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
    ```

**Pros of this approach:**
*   **Domain Purity:** The `User` aggregate has zero dependency on external services like `TimeProvider`. Its logic is pure and easy to unit test.
*   **Explicit is Better than Implicit:** The state change is explicit and fully contained within the `MarkAsDeleted` method. Anyone reading the method knows exactly what happens.
*   **Correct Responsibility:** The Application Layer is responsible for orchestrating domain objects with infrastructure concerns (like getting the current time).

---

Of course. This is a critical architectural consideration. Here is a comprehensive deletion strategy for every domain object defined in the `Domain_Design.md` document, applying the principles we've discussed.

---

### Comprehensive Deletion Strategy for All Domain Objects

| Domain Object (Type) | Deletion Strategy | Rationale & Implementation Details |
| :--- | :--- | :--- |
| **1. `User` (Aggregate Root)** | **Soft Delete + Anonymization** | **Reason:** A `User` is a central identity with Personally Identifiable Information (PII) linked to critical, immutable history (`Order`, `Review`). A hard delete would corrupt historical data. <br> **Implementation:** 1. An `IsDeleted` flag is set immediately. 2. A `UserDeleted` event triggers a background saga to scrub PII from associated records (`Order`, `Review`) and hard-delete ephemeral links (`RoleAssignment`). 3. The `User` record itself can be hard-deleted after the saga completes. |
| &nbsp;&nbsp;&nbsp; `Address` (Child Entity) | **Hard Delete (Cascading)** | **Reason:** An address contains PII and has no meaning or independent existence without its parent `User`. <br> **Implementation:** When the parent `User` record is finally hard-deleted by the anonymization saga, all its child addresses are deleted with it. |
| &nbsp;&nbsp;&nbsp; `PaymentMethod` (Child Entity)| **Hard Delete (Cascading)** | **Reason:** Same as `Address`. A tokenized payment method is PII-adjacent and is meaningless without a user. <br> **Implementation:** Hard-deleted along with the parent `User` at the end of the anonymization process. |
| **2. `RoleAssignment` (Aggregate Root)** | **Hard Delete** | **Reason:** This represents a current permission, not a historical fact. When access is revoked, the record should cease to exist. There is no business value in keeping a "soft-deleted" permission. <br> **Implementation:** A standard `DELETE` SQL statement. |
| **3. `Restaurant` (Aggregate Root)** | **Soft Delete** | **Reason:** A `Restaurant` is linked to immutable financial records (`Order`, `RestaurantAccount`). Hard-deleting it would orphan massive amounts of essential business data. <br> **Implementation:** Use a status field or flags like `IsActive` and `IsArchived`. An archived restaurant is hidden from all public views but its data is preserved for reporting. |
| **4. `Menu` (Independent Entity)** | **Soft Delete** | **Reason:** A restaurant owner may want to disable a menu (e.g., "Summer Specials") and re-enable it later. A hard delete provides a poor user experience. <br> **Implementation:** An `IsEnabled` or `IsArchived` flag. |
| &nbsp;&nbsp;&nbsp; `MenuCategory` (Independent Entity) | **Soft Delete** | **Reason:** Same as `Menu`. An owner might want to temporarily hide a category like "Holiday Desserts" without having to recreate it. <br> **Implementation:** An `IsEnabled` or `IsArchived` flag. |
| **5. `MenuItem` (Aggregate Root)** | **Soft Delete** | **Reason:** Essential for operational flexibility. An owner needs to mark items as "out of stock" (temporary `IsAvailable` flag) or "archived" (permanent soft delete) without losing the item's data. <br> **Implementation:** An `IsArchived` flag to signify it's removed from the menu builder, distinct from the `IsAvailable` flag for daily operations. |
| **6. `CustomizationGroup` (Aggregate Root)** | **Soft Delete** | **Reason:** These are reusable components. An owner might want to disable a group of toppings and reuse it later. <br> **Implementation:** An `IsArchived` flag. A background process should handle disassociating it from menu items cleanly. |
| &nbsp;&nbsp;&nbsp; `CustomizationChoice` (Child Entity)| **Hard Delete** | **Reason:** A choice ("Extra Cheese") is tightly coupled to its group ("Toppings"). If it's removed from the group, it should be permanently gone from that context. <br> **Implementation:** The `RemoveChoice` method on the aggregate root should remove the entity from its internal list, and the ORM will handle the `DELETE`. |
| **7. `Tag` (Independent Entity)** | **Soft Delete** | **Reason:** These are system-wide or restaurant-wide master data. Hard-deleting a tag like "Vegan" could break filters or data on many menu items. <br> **Implementation:** An admin should be able to "archive" a tag, preventing its use on new items. |
| **8. `RestaurantAccount` (Aggregate Root)** | **Immutable** | **Reason:** This is a financial ledger. Its existence and state are business facts. It can be marked as `Closed` when a restaurant is archived, but it can never be deleted. <br> **Implementation:** No delete functionality is exposed. A `CloseAccount()` method can change its state. |
| &nbsp;&nbsp;&nbsp; `AccountTransaction` (Independent Entity) | **Immutable** | **Reason:** This is the most sacred financial record. It is an immutable entry in a ledger, equivalent to a bank statement line. <br> **Implementation:** No delete or update functionality exists. Corrections are made by adding new, opposing transactions. |
| **9. `Order` (Aggregate Root)** | **Immutable** | **Reason:** An order is a legal and financial contract between a customer and a restaurant. It must be preserved permanently for accounting, reporting, and dispute resolution. <br> **Implementation:** No delete functionality. State is managed via transitions (e.g., `Place()`, `Accept()`, `Cancel()`). |
| &nbsp;&nbsp;&nbsp; `OrderItem` (Child Entity) | **Immutable** | **Reason:** Part of the immutable `Order` record. |
| &nbsp;&nbsp;&nbsp; `PaymentTransaction` (Child Entity)| **Immutable** | **Reason:** Part of the immutable `Order` record. |
| **10. `Coupon` (Aggregate Root)** | **Soft Delete** | **Reason:** A restaurant owner needs to see the performance of past coupons. They might also want to clone an old coupon. Hard-deleting removes this valuable historical context. <br> **Implementation:** An `IsEnabled` flag for pausing and an `IsArchived` flag for "deleting" it from the active list. |
| **11. `Review` (Aggregate Root)** | **Soft Delete** | **Reason:** A review is user-generated content that affects business metrics. It should not be hard-deleted by users to prevent rating manipulation. Admins need to be able to hide inappropriate content without losing the record of the moderation action. <br> **Implementation:** An `IsHidden` flag, toggled by a moderator. |
| **12. `SupportTicket` (Aggregate Root)** | **Immutable** | **Reason:** This is a record of a business interaction. It is an audit trail for customer service and must be preserved. <br> **Implementation:** No delete functionality. It transitions to a `Closed` state. |
| &nbsp;&nbsp;&nbsp; `TicketMessage` (Child Entity) | **Immutable** | **Reason:** A message in a conversation thread cannot be altered or deleted to maintain the integrity of the support record. |

**1. `User` (Aggregate Root)** | **Soft Delete + Anonymization**
**2. `RoleAssignment` (Aggregate Root)** | **Hard Delete** 
**3. `Restaurant` (Aggregate Root)** | **Soft Delete**
**4. `Menu` and `MenuCategory` (Independent Entity)** | **Soft Delete**
**5. `MenuItem` (Aggregate Root)** | **Soft Delete**
**6. `CustomizationGroup` (Aggregate Root)** | **Soft Delete**
**7. `Tag` (Independent Entity)** | **Soft Delete**
**8. `RestaurantAccount` (Aggregate Root)** and `AccountTransaction` (Independent Entity) | **Immutable**
**9. `Order` (Aggregate Root)** | **Immutable**
**10. `Coupon` (Aggregate Root)** | **Soft Delete**
**11. `Review` (Aggregate Root)** | **Soft Delete**
**12. `SupportTicket` (Aggregate Root)** | **Immutable**

All child entities (like `Address`, `PaymentMethod`, `OrderItem`, etc.) follow the deletion strategy of their parent aggregate. If the parent is soft-deleted, child entities are either hard-deleted or soft-deleted based on their nature.

For Soft Delete, implement the ISoftDeletableEntity and IAuditableEntity. For Immutable entities, implement the ICreationAuditable and ensure no delete or update methods are exposed. For Hard Delete, implement the ICreationAuditable and ensure no MarkAsDeleted method is present.