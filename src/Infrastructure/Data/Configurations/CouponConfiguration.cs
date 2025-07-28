using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Configurations.Common;

namespace YummyZoom.Infrastructure.Data.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        // --- 1. Primary Key ---
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => CouponId.Create(value));

        // --- 2. Simple Properties & Enums ---
        builder.Property(c => c.Code)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(c => new { c.Code, c.RestaurantId })
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.ValidityStartDate)
            .IsRequired();

        builder.Property(c => c.ValidityEndDate)
            .IsRequired();

        builder.Property(c => c.TotalUsageLimit)
            .IsRequired();

        builder.Property(c => c.CurrentTotalUsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.UsageLimitPerUser)
            .IsRequired();

        builder.Property(c => c.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        // --- 3. References to Other Aggregates (by ID) ---
        builder.Property(c => c.RestaurantId)
            .IsRequired()
            .HasConversion(id => id.Value, value => RestaurantId.Create(value));

        // --- 4. Owned Value Objects ---
        // Money VOs
        builder.OwnsOne(c => c.MinOrderAmount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("MinOrderAmount_Amount")
                .HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("MinOrderAmount_Currency")
                .HasMaxLength(3);
        });

        // Map CouponValue as owned entity with discriminator pattern
        builder.OwnsOne(c => c.Value, valueBuilder =>
        {
            valueBuilder.Property(v => v.Type)
                .HasColumnName("Value_Type")
                .HasConversion<string>()
                .IsRequired();
            
            valueBuilder.Property(v => v.PercentageValue)
                .HasColumnName("Value_PercentageValue")
                .HasColumnType("decimal(5,2)");
            
            valueBuilder.OwnsOne(v => v.FixedAmountValue, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                    .HasColumnName("Value_FixedAmount_Amount")
                    .HasColumnType("decimal(18,2)");
                moneyBuilder.Property(m => m.Currency)
                    .HasColumnName("Value_FixedAmount_Currency")
                    .HasMaxLength(3);
            });
            
            valueBuilder.Property(v => v.FreeItemValue)
                .HasColumnName("Value_FreeItemValue")
                .HasConversion(id => id != null ? id.Value : (Guid?)null, 
                              value => value.HasValue ? MenuItemId.Create(value.Value) : null);
        });

        // Map AppliesTo as owned VO with JSON for collections
        builder.OwnsOne(c => c.AppliesTo, appliesToBuilder =>
        {
            appliesToBuilder.Property(a => a.Scope)
                .HasColumnName("AppliesTo_Scope")
                .HasConversion<string>()
                .IsRequired();
            
            appliesToBuilder.Property(a => a.ItemIds)
                .HasColumnName("AppliesTo_ItemIds")
                .HasColumnType("jsonb") 
                .HasConversion(
                    itemIds => JsonSerializer.Serialize(itemIds.Select(id => id.Value).ToList(), (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!
                        .Select(MenuItemId.Create).ToList().AsReadOnly(),
                    new ValueComparer<IReadOnlyList<MenuItemId>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList().AsReadOnly()));
            
            appliesToBuilder.Property(a => a.CategoryIds)
                .HasColumnName("AppliesTo_CategoryIds")
                .HasColumnType("jsonb") 
                .HasConversion(
                    categoryIds => JsonSerializer.Serialize(categoryIds.Select(id => id.Value).ToList(), (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!
                        .Select(MenuCategoryId.Create).ToList().AsReadOnly(),
                    new ValueComparer<IReadOnlyList<MenuCategoryId>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList().AsReadOnly()));
        });

        // --- 5. Auditing & Soft Delete ---
        builder.ConfigureAuditProperties();
        builder.ConfigureSoftDeleteProperties();
    }
}
