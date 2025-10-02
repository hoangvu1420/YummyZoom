using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus");

        // --- 1. Primary Key ---
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => MenuId.Create(value));

        // --- 2. Simple Properties & Enums ---
        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.IsEnabled)
            .IsRequired();

        // --- 3. References to Other Aggregates (by ID) ---
        builder.Property(m => m.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // --- 4. Auditing & Soft Delete ---
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();

        // --- 5. Indexes ---
        builder.HasIndex(m => m.RestaurantId)
            .HasDatabaseName("IX_Menus_RestaurantId");
    }
}
