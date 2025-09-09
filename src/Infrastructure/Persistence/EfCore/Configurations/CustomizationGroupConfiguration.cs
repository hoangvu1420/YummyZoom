using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore.Configurations.Common;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class CustomizationGroupConfiguration : IEntityTypeConfiguration<CustomizationGroup>
{
    public void Configure(EntityTypeBuilder<CustomizationGroup> builder)
    {
        builder.ToTable("CustomizationGroups");

        // Primary key
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => CustomizationGroupId.Create(value));

        // References to other aggregates (by ID only)
        builder.Property(g => g.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // Simple properties
        builder.Property(g => g.GroupName)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(g => g.MinSelections).IsRequired();
        builder.Property(g => g.MaxSelections).IsRequired();

        // Child entity collection: Choices
        builder.OwnsMany(g => g.Choices, choiceBuilder =>
        {
            choiceBuilder.ToTable("CustomizationChoices");
            choiceBuilder.WithOwner().HasForeignKey("CustomizationGroupId");

            // Key
            choiceBuilder.HasKey("CustomizationGroupId", "Id");
            choiceBuilder.Property(c => c.Id)
                .HasColumnName("ChoiceId")
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, value => ChoiceId.Create(value));

            // Properties
            choiceBuilder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
            choiceBuilder.Property(c => c.IsDefault)
                .IsRequired();
            choiceBuilder.Property(c => c.DisplayOrder)
                .IsRequired();

            // Value Object: Money for PriceAdjustment
            choiceBuilder.OwnsOne(c => c.PriceAdjustment, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("PriceAdjustment_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("PriceAdjustment_Currency")
                    .HasMaxLength(3);
            });
        });

        // Auditing and soft delete
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();

        // Use field access for the collection to respect encapsulation
        builder.Metadata.FindNavigation(nameof(CustomizationGroup.Choices))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}


