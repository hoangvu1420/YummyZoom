using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;

/// <summary>
/// Validates coupon bundle data before seeding.
/// </summary>
public static class CouponBundleValidation
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();

        public void AddError(string error) => Errors.Add(error);
    }

    /// <summary>
    /// Validates a coupon bundle and returns validation result with any errors found.
    /// </summary>
    public static ValidationResult Validate(CouponBundle bundle)
    {
        var result = new ValidationResult();

        // Required fields at bundle level
        if (string.IsNullOrWhiteSpace(bundle.RestaurantSlug))
            result.AddError("RestaurantSlug is required");

        if (bundle.Coupons == null || bundle.Coupons.Count == 0)
            result.AddError("Coupons array is required and must contain at least one coupon");
        else
        {
            // Validate each coupon
            for (int i = 0; i < bundle.Coupons.Count; i++)
            {
                var coupon = bundle.Coupons[i];
                var prefix = $"Coupon[{i}]";
                
                ValidateCoupon(coupon, prefix, result);
            }
        }

        return result;
    }

    private static void ValidateCoupon(CouponData coupon, string prefix, ValidationResult result)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(coupon.Code))
            result.AddError($"{prefix}: Code is required");
        else if (coupon.Code.Length > 50)
            result.AddError($"{prefix}: Code cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(coupon.Description))
            result.AddError($"{prefix}: Description is required");

        // Value type validation
        if (string.IsNullOrWhiteSpace(coupon.ValueType))
        {
            result.AddError($"{prefix}: ValueType is required");
        }
        else
        {
            var validValueTypes = new[] { "Percentage", "FixedAmount", "FreeItem" };
            if (!validValueTypes.Contains(coupon.ValueType, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError($"{prefix}: ValueType must be one of: {string.Join(", ", validValueTypes)}");
            }
            else
            {
                // Validate type-specific fields
                switch (coupon.ValueType.ToLowerInvariant())
                {
                    case "percentage":
                        if (!coupon.Percentage.HasValue)
                            result.AddError($"{prefix}: Percentage is required when ValueType is 'Percentage'");
                        else if (coupon.Percentage.Value <= 0 || coupon.Percentage.Value > 100)
                            result.AddError($"{prefix}: Percentage must be between 1 and 100");
                        break;

                    case "fixedamount":
                        if (!coupon.FixedAmount.HasValue)
                            result.AddError($"{prefix}: FixedAmount is required when ValueType is 'FixedAmount'");
                        else if (coupon.FixedAmount.Value <= 0)
                            result.AddError($"{prefix}: FixedAmount must be greater than 0");

                        if (string.IsNullOrWhiteSpace(coupon.FixedCurrency))
                            result.AddError($"{prefix}: FixedCurrency is required when ValueType is 'FixedAmount'");
                        else if (coupon.FixedCurrency.Length != 3)
                            result.AddError($"{prefix}: FixedCurrency must be a 3-letter code (e.g., VND, USD)");
                        break;

                    case "freeitem":
                        if (string.IsNullOrWhiteSpace(coupon.FreeItemName))
                            result.AddError($"{prefix}: FreeItemName is required when ValueType is 'FreeItem'");
                        break;
                }
            }
        }

        // Scope validation
        if (string.IsNullOrWhiteSpace(coupon.Scope))
        {
            result.AddError($"{prefix}: Scope is required");
        }
        else
        {
            var validScopes = new[] { "WholeOrder", "SpecificItems", "SpecificCategories" };
            if (!validScopes.Contains(coupon.Scope, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError($"{prefix}: Scope must be one of: {string.Join(", ", validScopes)}");
            }
            else
            {
                // Validate scope-specific fields
                switch (coupon.Scope.ToLowerInvariant())
                {
                    case "specificitems":
                        if (coupon.ItemNames == null || coupon.ItemNames.Count == 0)
                            result.AddError($"{prefix}: ItemNames is required and must have at least one item when Scope is 'SpecificItems'");
                        else if (coupon.ItemNames.Any(string.IsNullOrWhiteSpace))
                            result.AddError($"{prefix}: All ItemNames must be non-empty");
                        break;

                    case "specificcategories":
                        if (coupon.CategoryNames == null || coupon.CategoryNames.Count == 0)
                            result.AddError($"{prefix}: CategoryNames is required and must have at least one category when Scope is 'SpecificCategories'");
                        else if (coupon.CategoryNames.Any(string.IsNullOrWhiteSpace))
                            result.AddError($"{prefix}: All CategoryNames must be non-empty");
                        break;
                }
            }
        }

        // Validity period
        if (coupon.ValidityEndDate <= coupon.ValidityStartDate)
            result.AddError($"{prefix}: ValidityEndDate must be after ValidityStartDate");

        // Minimum order amount
        if (coupon.MinOrderAmount.HasValue)
        {
            if (coupon.MinOrderAmount.Value <= 0)
                result.AddError($"{prefix}: MinOrderAmount must be greater than 0 when specified");

            if (string.IsNullOrWhiteSpace(coupon.MinOrderCurrency))
                result.AddError($"{prefix}: MinOrderCurrency is required when MinOrderAmount is specified");
            else if (coupon.MinOrderCurrency.Length != 3)
                result.AddError($"{prefix}: MinOrderCurrency must be a 3-letter code (e.g., VND, USD)");
        }

        // Usage limits
        if (coupon.TotalUsageLimit.HasValue && coupon.TotalUsageLimit.Value <= 0)
            result.AddError($"{prefix}: TotalUsageLimit must be greater than 0 when specified");

        if (coupon.UsageLimitPerUser.HasValue && coupon.UsageLimitPerUser.Value <= 0)
            result.AddError($"{prefix}: UsageLimitPerUser must be greater than 0 when specified");
    }
}

