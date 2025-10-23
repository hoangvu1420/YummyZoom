using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public class ActiveCouponViewConfiguration : IEntityTypeConfiguration<ActiveCouponView>
{
    public void Configure(EntityTypeBuilder<ActiveCouponView> builder)
    {
        // Map to the materialized view
        builder.ToView("active_coupons_view");
        
        // Configure primary key (required by EF)
        builder.HasKey(x => x.CouponId);
        
        // Map columns to match the materialized view
        builder.Property(x => x.CouponId).HasColumnName("coupon_id");
        builder.Property(x => x.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(x => x.Code).HasColumnName("code");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.ValueType).HasColumnName("value_type");
        builder.Property(x => x.PercentageValue).HasColumnName("percentage_value");
        builder.Property(x => x.FixedAmountValue).HasColumnName("fixed_amount_value");
        builder.Property(x => x.FixedAmountCurrency).HasColumnName("fixed_amount_currency");
        builder.Property(x => x.FreeItemId).HasColumnName("free_item_id");
        builder.Property(x => x.AppliesToScope).HasColumnName("applies_to_scope");
        builder.Property(x => x.AppliesToItemIds).HasColumnName("applies_to_item_ids");
        builder.Property(x => x.AppliesToCategoryIds).HasColumnName("applies_to_category_ids");
        builder.Property(x => x.MinOrderAmount).HasColumnName("min_order_amount");
        builder.Property(x => x.MinOrderCurrency).HasColumnName("min_order_currency");
        builder.Property(x => x.ValidityStartDate).HasColumnName("validity_start_date");
        builder.Property(x => x.ValidityEndDate).HasColumnName("validity_end_date");
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        builder.Property(x => x.TotalUsageLimit).HasColumnName("total_usage_limit");
        builder.Property(x => x.UsageLimitPerUser).HasColumnName("usage_limit_per_user");
        builder.Property(x => x.CurrentTotalUsageCount).HasColumnName("current_total_usage_count");
        builder.Property(x => x.LastRefreshedAt).HasColumnName("last_refreshed_at");
    }
}
