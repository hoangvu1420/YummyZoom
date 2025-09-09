using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");

        // --- 1. Primary Key ---
        builder.HasKey(mc => mc.Id);
        builder.Property(mc => mc.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => MenuCategoryId.Create(value));

        // --- 2. Simple Properties & Enums ---
        builder.Property(mc => mc.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(mc => mc.DisplayOrder)
            .IsRequired();

        // --- 3. References to Other Aggregates (by ID) ---
        builder.Property(mc => mc.MenuId)
            .IsRequired()
            .HasConversion(id => id.Value, value => MenuId.Create(value));

        // --- 4. Auditing & Soft Delete ---
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();

        // --- 5. Indexes ---
        builder.HasIndex(mc => mc.MenuId)
            .HasDatabaseName("IX_MenuCategories_MenuId");
        
        // Add index on DisplayOrder for ordered retrieval
        builder.HasIndex(mc => new { mc.MenuId, mc.DisplayOrder })
            .HasDatabaseName("IX_MenuCategories_MenuId_DisplayOrder");
    }
}
