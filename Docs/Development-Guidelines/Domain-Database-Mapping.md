### General Best Practices for Mapping DDD Aggregates with EF Core

You've already implemented most of these in your `UserConfiguration`, but let's formalize them into a set of principles you can apply everywhere.

### Configuration Best Practices

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

7. **Map Collections of Primitive/VOs:** For simple collections like `MenuItem.DietaryTagIds` (a list of `TagId`s), use the **standardized JSONB approach** with our reusable infrastructure:

    **Recommended Approach: Use Shared JSONB Infrastructure**

    We've implemented a reusable pattern that handles JSON serialization consistently across all aggregates using shared `JsonSerializerOptions` and extension methods.

    **Step 1: Use the Extension Method**
    ```csharp
    // In your entity configuration (e.g., MenuItemConfiguration.cs)
    builder.Property(mi => mi.DietaryTagIds).HasJsonbListConversion<TagId>();
    builder.Property(mi => mi.AppliedCustomizations).HasJsonbListConversion<AppliedCustomization>();
    ```

    **Step 2: Infrastructure Setup (Already Implemented)**
    
    The shared infrastructure consists of:
    - `DomainJson.cs`: Provides consistent `JsonSerializerOptions` with domain-specific converters
    - `EfJsonbExtensions.cs`: Reusable extension method for JSONB list conversion with proper `ValueComparer`

    **Implementation Details:**
    ```csharp
    // src/Infrastructure/Serialization/DomainJson.cs
    public static class DomainJson
    {
        public static JsonSerializerOptions Options { get; } = new()
        {
            Converters = { new AggregateRootIdJsonConverterFactory() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // src/Infrastructure/Data/Configurations/Common/EfJsonbExtensions.cs
    public static PropertyBuilder<IReadOnlyList<T>> HasJsonbListConversion<T>(this PropertyBuilder<IReadOnlyList<T>> propertyBuilder)
    {
        return propertyBuilder
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, DomainJson.Options),
                v => JsonSerializer.Deserialize<IReadOnlyList<T>>(v, DomainJson.Options) ?? new List<T>(),
                new ValueComparer<IReadOnlyList<T>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c?.Aggregate(0, (a, v) => HashCode.Combine(a, v?.GetHashCode() ?? 0)) ?? 0,
                    c => c?.ToList() ?? new List<T>()));
    }
    ```

    **Benefits of This Approach:**
    - ✅ Consistent JSON serialization across all aggregates
    - ✅ Proper handling of strongly-typed IDs with `AggregateRootIdJsonConverterFactory`
    - ✅ Reusable extension method reduces code duplication
    - ✅ Null-safe `ValueComparer` handles edge cases
    - ✅ Type-safe implementation prevents runtime errors

    **Important Note for Value Objects in JSON Columns:**
    When creating value objects that will be stored in JSON columns, ensure they have proper JSON deserialization support:
    - Add `[JsonConstructor]` attribute to the parameterized constructor
    - Constructor parameter names must match property names exactly (case-insensitive)
    - Include `using System.Text.Json.Serialization;` directive
    - Provide a parameterless constructor for EF Core (marked as `internal`)

    **Alternative Approach: Join Tables**
    For relationships requiring querying or complex operations, use join tables. This is more complex but necessary for certain use cases.

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

#### 2. `MenuItem` Aggregate Configuration

This example demonstrates the new standardized JSONB approach for collections:

```csharp
// src/Infrastructure/Data/Configurations/MenuItemConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Configurations.Common;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(mi => mi.Id);
        builder.Property(mi => mi.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => MenuItemId.Create(value));

        // --- Basic Properties ---
        builder.Property(mi => mi.Name).HasMaxLength(200).IsRequired();
        builder.Property(mi => mi.Description).HasMaxLength(1000);
        
        // --- Money Value Object ---
        builder.OwnsOne(mi => mi.BasePrice, priceBuilder =>
        {
            priceBuilder.Property(p => p.Amount).HasColumnName("BasePrice").HasColumnType("decimal(18,2)");
            priceBuilder.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        // --- Foreign Key References ---
        builder.Property(mi => mi.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        builder.Property(mi => mi.MenuCategoryId)
            .IsRequired()
            .HasConversion(id => id.Value, value => MenuCategoryId.Create(value));

        // --- JSONB Collections using Standardized Approach ---
        builder.Property(mi => mi.DietaryTagIds).HasJsonbListConversion<TagId>();
        builder.Property(mi => mi.AppliedCustomizations).HasJsonbListConversion<AppliedCustomization>();

        // --- Other Properties ---
        builder.Property(mi => mi.IsAvailable).IsRequired();
        builder.Property(mi => mi.CreatedDateTime).IsRequired();
        builder.Property(mi => mi.UpdatedDateTime).IsRequired();
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
