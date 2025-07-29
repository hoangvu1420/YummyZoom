using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        // --- 1. Primary Key ---
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => MenuItemId.Create(value));

        // --- 2. References to other aggregates ---
        builder.Property(m => m.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        builder.Property(m => m.MenuCategoryId)
            .IsRequired()
            .HasConversion(id => id.Value, value => MenuCategoryId.Create(value));

        // --- 3. Simple Properties ---
        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(m => m.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        // --- 4. Owned Value Objects ---
        
        // Map the BasePrice (Money) Value Object using OwnsOne
        builder.OwnsOne(m => m.BasePrice, priceBuilder =>
        {
            priceBuilder.Property(p => p.Amount)
                .HasColumnName("BasePrice_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            priceBuilder.Property(p => p.Currency)
                .HasColumnName("BasePrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // --- 5. Collections ---

        // Map DietaryTagIds as JSONB column
        builder.Property(m => m.DietaryTagIds)
            .HasColumnType("jsonb")
            .HasConversion(
                tagIds => JsonSerializer.Serialize(tagIds.Select(id => id.Value).ToList(), (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!
                    .Select(TagId.Create).ToList(),
                new ValueComparer<IReadOnlyList<TagId>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );

        // Configure collection of Value Objects as a JSONB column.
        builder.Property(m => m.AppliedCustomizations)
            .HasColumnType("jsonb") 
            .HasConversion(
                customizations => JsonSerializer.Serialize(customizations, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<AppliedCustomization>>(json, (JsonSerializerOptions?)null)!,
                new ValueComparer<IReadOnlyList<AppliedCustomization>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );
            
        // --- 6. Indexes ---
        builder.HasIndex(m => m.RestaurantId)
            .HasDatabaseName("IX_MenuItems_RestaurantId");

        builder.HasIndex(m => m.MenuCategoryId)
            .HasDatabaseName("IX_MenuItems_MenuCategoryId");

        builder.HasIndex(m => m.Name)
            .HasDatabaseName("IX_MenuItems_Name");

        builder.HasIndex(m => m.IsAvailable)
            .HasDatabaseName("IX_MenuItems_IsAvailable");

        // Composite indexes for common queries
        builder.HasIndex(m => new { m.RestaurantId, m.IsAvailable })
            .HasDatabaseName("IX_MenuItems_Restaurant_Available");

        builder.HasIndex(m => new { m.MenuCategoryId, m.IsAvailable })
            .HasDatabaseName("IX_MenuItems_Category_Available");

        // --- 7. Auditing & Soft Delete Properties ---
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();
    }
}
