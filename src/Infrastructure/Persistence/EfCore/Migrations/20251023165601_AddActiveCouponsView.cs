using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveCouponsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create materialized view for active coupons optimized for fast coupon check
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW active_coupons_view AS
                SELECT 
                    c.""Id"" as coupon_id,
                    c.""RestaurantId"" as restaurant_id,
                    c.""Code"" as code,
                    c.""Description"" as description,
                    c.""Value_Type"" as value_type,
                    c.""Value_PercentageValue"" as percentage_value,
                    c.""Value_FixedAmount_Amount"" as fixed_amount_value,
                    c.""Value_FixedAmount_Currency"" as fixed_amount_currency,
                    c.""Value_FreeItemValue"" as free_item_id,
                    c.""AppliesTo_Scope"" as applies_to_scope,
                    c.""AppliesTo_ItemIds"" as applies_to_item_ids,
                    c.""AppliesTo_CategoryIds"" as applies_to_category_ids,
                    c.""MinOrderAmount_Amount"" as min_order_amount,
                    c.""MinOrderAmount_Currency"" as min_order_currency,
                    c.""ValidityStartDate"" as validity_start_date,
                    c.""ValidityEndDate"" as validity_end_date,
                    c.""IsEnabled"" as is_enabled,
                    c.""TotalUsageLimit"" as total_usage_limit,
                    c.""UsageLimitPerUser"" as usage_limit_per_user,
                    c.""CurrentTotalUsageCount"" as current_total_usage_count,
                    NOW() as last_refreshed_at
                FROM ""Coupons"" c
                WHERE c.""IsEnabled"" = true 
                  AND c.""IsDeleted"" = false
                  AND c.""ValidityEndDate"" >= NOW();
            ");

            // Create indexes for optimal query performance
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX idx_active_coupons_view_id 
                ON active_coupons_view (coupon_id);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX idx_active_coupons_view_restaurant 
                ON active_coupons_view (restaurant_id, validity_end_date, code);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX idx_active_coupons_view_validity 
                ON active_coupons_view (validity_start_date, validity_end_date);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_active_coupons_view_validity;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_active_coupons_view_restaurant;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_active_coupons_view_id;");
            
            // Drop materialized view
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS active_coupons_view;");
        }
    }
}
