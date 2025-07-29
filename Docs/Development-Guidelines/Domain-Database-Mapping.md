### General Best Practices for Mapping DDD Aggregates with EF Core

You've already implemented most of these in your `UserConfiguration`, but let's formalize them into a set of principles you can apply everywhere.

1. **One Configuration File per Aggregate Root/Independent Entity:** Just as you did with `UserConfiguration`, create a separate `IEntityTypeConfiguration<T>` for each aggregate root (e.g., `Restaurant`, `Order`, `MenuItem`) and each "Independent Entity" (e.g., `Tag`, `AccountTransaction`). This keeps your mapping logic organized and decoupled from the `DbContext`.

2. **Explicit Table Naming:** Always use `builder.ToTable("TableName")` to explicitly name your tables. This avoids EF Core's default pluralization rules, giving you full control and clarity.

3. **Configure Strongly-Typed IDs:** For every strongly-typed ID (e.g., `RestaurantId`, `OrderId`), consistently use the `HasConversion` method. The key is to use `ValueGeneratedNever()` for the aggregate root's ID, as the domain is responsible for creating it (`Guid.NewGuid()` or a Hi/Lo algorithm), not the database.

    ```csharp
    // For the aggregate root's primary key
    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .ValueGeneratedNever() // The domain creates the ID.
        .HasConversion(
            id => id.Value,
            value => YourIdType.Create(value));
    ```

4. **Map Child Entities with `OwnsMany`:** For collections of child entities that only exist as part of the aggregate (like `User.Addresses` or `Order.OrderItems`), `OwnsMany` is the perfect tool.
    * Give the owned collection its own table with `addressBuilder.ToTable("UserAddresses")`.
    * Establish the relationship with `addressBuilder.WithOwner().HasForeignKey("UserId")`.
    * If the child entity has its own identity within the aggregate (like your `Address` with `AddressId`), configure its key using `addressBuilder.HasKey(a => a.Id)`.

5. **Map Value Objects (VOs) with `OwnsOne`:** For single value objects that represent a concept without identity (like `Restaurant.Location`), use `OwnsOne`. The properties of the VO will be mapped as columns on the parent table (e.g., `Location_Street`, `Location_City`).

    ```csharp
    builder.OwnsOne(r => r.Location, locationBuilder =>
    {
        locationBuilder.Property(a => a.Street).HasColumnName("Street").IsRequired();
        locationBuilder.Property(a => a.City).HasColumnName("City").IsRequired();
        // ... and so on
    });
    ```

6. **Reference Other Aggregates by ID Only:** Your design correctly states that aggregates should only reference each other by their ID. **Do not create navigation properties (e.g., `public virtual Order Order { get; set; }`) for other aggregates.**
    * Simply map the foreign key property.
    * Use the same `HasConversion` pattern for these foreign key IDs.
    * You can optionally configure the relationship with `HasOne`/`WithMany` to enforce database-level foreign key constraints, but it's not strictly necessary for reads/writes if your application services correctly manage IDs.

7. **Map Collections of Primitive/VOs:** For simple collections like `MenuItem.DietaryTagIDs` (a list of `TagID`s), you have two main options:

    * **A. Recommended Approach: Use a JSON/JSONB Column**

        This approach treats the collection of VOs as a single, atomic attribute of the parent entity, which aligns perfectly with DDD principles.

        **How:** Serialize the `List<T>` or `IReadOnlyList<T>` into a JSON string and store it in a single `jsonb` (for PostgreSQL) or `nvarchar(max)` (for SQL Server) column. Use EF Core's `HasConversion` feature combined with a `ValueComparer`.

        **Example Code (for a `List<TagId>` on a `MenuItem`):**

        ```csharp
        builder.Property(mi => mi.DietaryTagIDs)
            .HasColumnType("jsonb") // Be explicit for PostgreSQL for performance and features
            .HasConversion(
                // To the database: Serialize the list to a JSON string
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                // From the database: Deserialize the JSON string back to a list
                v => JsonSerializer.Deserialize<List<TagID>>(v, (JsonSerializerOptions)null),
                // A ValueComparer helps EF Core's change tracker efficiently detect changes to the list
                new ValueComparer<List<TagID>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
        ```

        **⚠️ Important: Type Consistency Warning**

        When using `HasConversion` with `ValueComparer` for collections, ensure the generic type in `ValueComparer<T>` exactly matches the property type in your domain entity. For example:
        * If your property is `IReadOnlyList<AppliedCustomization>`, use `ValueComparer<IReadOnlyList<AppliedCustomization>>`
        * If your property is `List<AppliedCustomization>`, use `ValueComparer<List<AppliedCustomization>>`

        A type mismatch will cause a `System.InvalidOperationException` at runtime with a message like "ValueComparer for 'List<T>' cannot be used for 'IReadOnlyList<T>'".

    * **B. Alternative Approach: Use a Join Table**
        The more traditional relational approach, creating a `MenuItemTags` table. This is more complex to set up and is often overkill.

8. **Use `.HasConversion<string>()` for Enums:** Storing enums as strings in the database is far more readable and resilient to changes in the enum's integer values.

---

### Applying the Practices: Example Configurations

Here’s how you would create the configuration files for some of your other key domain objects.

#### 1. `Restaurant` Aggregate Configuration

This aggregate is lean and contains Value Objects, making it a perfect candidate for `OwnsOne`.

```csharp
// src/Infrastructure/Data/Configurations/RestaurantConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects; // Assuming these exist

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.CuisineType).HasMaxLength(200); // Or use JSON conversion for a list

        // Map the Location Value Object using OwnsOne
        // This will create columns like Location_Street, Location_City etc. in the Restaurants table
        builder.OwnsOne(r => r.Location, locationBuilder =>
        {
            locationBuilder.Property(a => a.Street).HasMaxLength(255).IsRequired();
            locationBuilder.Property(a => a.City).HasMaxLength(100).IsRequired();
            locationBuilder.Property(a => a.State).HasMaxLength(100);
            locationBuilder.Property(a => a.ZipCode).HasMaxLength(20).IsRequired();
            locationBuilder.Property(a => a.Country).HasMaxLength(100).IsRequired();
        });

        // Map the ContactInfo Value Object
        builder.OwnsOne(r => r.ContactInfo, contactBuilder =>
        {
            contactBuilder.Property(c => c.PhoneNumber).HasMaxLength(50).HasColumnName("ContactPhoneNumber");
            contactBuilder.Property(c => c.Email).HasMaxLength(255).HasColumnName("ContactEmail");
        });

        // Map BusinessHours (assuming it's a structured VO, OwnsOne or JSON is good)
        builder.Property(r => r.BusinessHours).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<BusinessHoursVO>(v, (JsonSerializerOptions)null)
        );
    }
}
```

#### 2. `Order` Aggregate Configuration

This is a complex aggregate with child entities (`OrderItem`), owned VOs (`DeliveryAddress`, `Financials`), and references to other aggregates.

```csharp
// src/Infrastructure/Data/Configurations/OrderConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects; // For UserId
using YummyZoom.Domain.RestaurantAggregate.ValueObjects; // For RestaurantId
using YummyZoom.Domain.CouponAggregate.ValueObjects; // For CouponId

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => OrderId.Create(value));

        // --- References to other aggregates ---
        builder.Property(o => o.CustomerID)
            .IsRequired()
            .HasConversion(id => id.Value, value => UserId.Create(value));

        builder.Property(o => o.RestaurantID)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));
        
        builder.Property(o => o.AppliedCouponID)
            .HasConversion(id => id.Value, value => CouponId.Create(value)); // Handle optional ID

        // --- Simple Properties ---
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.OrderNumber).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        // --- Owned Value Objects (Snapshot & Financials) ---
        builder.OwnsOne(o => o.DeliveryAddress, addressBuilder => { /* configure like in RestaurantConfiguration */ });

        // Group financial properties into a component using OwnsOne for clarity
        builder.OwnsOne(o => o.Financials, financialsBuilder =>
        {
            financialsBuilder.Property(f => f.Subtotal).HasColumnType("decimal(18,2)");
            financialsBuilder.Property(f => f.DiscountAmount).HasColumnType("decimal(18,2)");
            financialsBuilder.Property(f => f.DeliveryFee).HasColumnType("decimal(18,2)");
            financialsBuilder.Property(f => f.TipAmount).HasColumnType("decimal(18,2)");
            financialsBuilder.Property(f => f.TaxAmount).HasColumnType("decimal(18,2)");
            financialsBuilder.Property(f => f.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // --- Owned Child Entity Collection: OrderItem ---
        builder.OwnsMany(o => o.OrderItems, itembuilder =>
        {
            itembuilder.ToTable("OrderItems");
            itembuilder.WithOwner().HasForeignKey("OrderId");
            itembuilder.HasKey(oi => oi.Id);

            itembuilder.Property(oi => oi.Id)
                .HasColumnName("OrderItemId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => OrderItemId.Create(value));
            
            // Snapshot properties are just regular mapped properties
            itembuilder.Property(oi => oi.Snapshot_MenuItemID).IsRequired();
            itembuilder.Property(oi => oi.Snapshot_ItemName).IsRequired().HasMaxLength(200);
            
            // Map Money VO for prices
            itembuilder.OwnsOne(oi => oi.Snapshot_BasePriceAtOrder, moneyBuilder => { /* ... */ });
            itembuilder.OwnsOne(oi => oi.LineItemTotal, moneyBuilder => { /* ... */ });

            // Snapshot of selected customizations can be stored as JSON
            itembuilder.Property(oi => oi.SelectedCustomizations)
                .HasColumnType("jsonb") 
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<OrderItemCustomization>>(v, (JsonSerializerOptions)null)!,
                    new ValueComparer<List<OrderItemCustomization>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                    )
                );
        });
        
        // Configure PaymentTransactions similarly to OrderItems if they are child entities
        builder.OwnsMany(o => o.PaymentTransactions, ptBuilder => { /* ... */ });
    }
}
```

#### 3. `RoleAssignment` Aggregate Configuration

This is a simple mapping table that links two other aggregates. The key here is the composite unique index.

```csharp
// src/Infrastructure/Data/Configurations/RoleAssignmentConfiguration.cs

public class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");

        builder.HasKey(ra => ra.Id);
        builder.Property(ra => ra.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => RoleAssignmentId.Create(value));

        // Map Foreign Key IDs
        builder.Property(ra => ra.UserID)
            .IsRequired()
            .HasConversion(id => id.Value, value => UserId.Create(value));

        builder.Property(ra => ra.RestaurantID)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));
        
        builder.Property(ra => ra.Role).HasConversion<string>().HasMaxLength(50);

        // Enforce the business rule: A user can only have one role per restaurant.
        builder.HasIndex(ra => new { ra.UserID, ra.RestaurantID }).IsUnique();
    }
}
```

#### 4. `AccountTransaction` (Independent Entity) Configuration

This entity is an immutable record. It's not an aggregate root in the traditional sense, but it has its own lifecycle and table.

```csharp
// src/Infrastructure/Data/Configurations/AccountTransactionConfiguration.cs

public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("AccountTransactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever() // Or use .ValueGeneratedOnAdd() if domain doesn't create it
            .HasConversion(id => id.Value, value => AccountTransactionId.Create(value));

        builder.Property(t => t.RestaurantAccountID).IsRequired()
            .HasConversion(id => id.Value, value => RestaurantAccountId.Create(value));
        
        builder.Property(t => t.RelatedOrderID)
            .HasConversion(id => id.Value, value => OrderId.Create(value));

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        
        builder.OwnsOne(t => t.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Value).HasColumnName("Amount").HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        builder.Property(t => t.Timestamp).IsRequired();
    }
}
```

### Final Step: Update `ApplicationDbContext`

For every new aggregate root or independent entity you create a configuration for, remember to add a `DbSet<T>` to your `ApplicationDbContext`.

```csharp
// In ApplicationDbContext.cs
public DbSet<Restaurant> Restaurants => Set<Restaurant>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<MenuItem> MenuItems => Set<MenuItem>();
public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
public DbSet<Coupon> Coupons => Set<Coupon>();
public DbSet<Review> Reviews => Set<Review>();
public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
public DbSet<RestaurantAccount> RestaurantAccounts => Set<RestaurantAccount>();
public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
public DbSet<Tag> Tags => Set<Tag>();
public DbSet<TeamCart> TeamCarts => Set<TeamCart>();
// Note: You don't need DbSets for Menu and MenuCategory if they are just organizational
// and always accessed via MenuItems or other aggregates. If you need to query them directly, add DbSets.
```

By following these patterns consistently, you will create a data access layer that is a clean, robust, and accurate reflection of your rich domain model.
