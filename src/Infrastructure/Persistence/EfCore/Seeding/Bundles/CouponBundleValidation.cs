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

        // Required fields
        if (string.IsNullOrWhiteSpace(bundle.RestaurantSlug))
            result.AddError("RestaurantSlug is required");

        if (string.IsNullOrWhiteSpace(bundle.Code))
            result.AddError("Code is required");
        else if (bundle.Code.Length > 50)
            result.AddError("Code cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(bundle.Description))
            result.AddError("Description is required");

        // Value type validation
        if (string.IsNullOrWhiteSpace(bundle.ValueType))
        {
            result.AddError("ValueType is required");
        }
        else
        {
            var validValueTypes = new[] { "Percentage", "FixedAmount", "FreeItem" };
            if (!validValueTypes.Contains(bundle.ValueType, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError($"ValueType must be one of: {string.Join(", ", validValueTypes)}");
            }
            else
            {
                // Validate type-specific fields
                switch (bundle.ValueType.ToLowerInvariant())
                {
                    case "percentage":
                        if (!bundle.Percentage.HasValue)
                            result.AddError("Percentage is required when ValueType is 'Percentage'");
                        else if (bundle.Percentage.Value <= 0 || bundle.Percentage.Value > 100)
                            result.AddError("Percentage must be between 1 and 100");
                        break;

                    case "fixedamount":
                        if (!bundle.FixedAmount.HasValue)
                            result.AddError("FixedAmount is required when ValueType is 'FixedAmount'");
                        else if (bundle.FixedAmount.Value <= 0)
                            result.AddError("FixedAmount must be greater than 0");

                        if (string.IsNullOrWhiteSpace(bundle.FixedCurrency))
                            result.AddError("FixedCurrency is required when ValueType is 'FixedAmount'");
                        else if (bundle.FixedCurrency.Length != 3)
                            result.AddError("FixedCurrency must be a 3-letter code (e.g., VND, USD)");
                        break;

                    case "freeitem":
                        if (string.IsNullOrWhiteSpace(bundle.FreeItemName))
                            result.AddError("FreeItemName is required when ValueType is 'FreeItem'");
                        break;
                }
            }
        }

        // Scope validation
        if (string.IsNullOrWhiteSpace(bundle.Scope))
        {
            result.AddError("Scope is required");
        }
        else
        {
            var validScopes = new[] { "WholeOrder", "SpecificItems", "SpecificCategories" };
            if (!validScopes.Contains(bundle.Scope, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError($"Scope must be one of: {string.Join(", ", validScopes)}");
            }
            else
            {
                // Validate scope-specific fields
                switch (bundle.Scope.ToLowerInvariant())
                {
                    case "specificitems":
                        if (bundle.ItemNames == null || bundle.ItemNames.Count == 0)
                            result.AddError("ItemNames is required and must have at least one item when Scope is 'SpecificItems'");
                        else if (bundle.ItemNames.Any(string.IsNullOrWhiteSpace))
                            result.AddError("All ItemNames must be non-empty");
                        break;

                    case "specificcategories":
                        if (bundle.CategoryNames == null || bundle.CategoryNames.Count == 0)
                            result.AddError("CategoryNames is required and must have at least one category when Scope is 'SpecificCategories'");
                        else if (bundle.CategoryNames.Any(string.IsNullOrWhiteSpace))
                            result.AddError("All CategoryNames must be non-empty");
                        break;
                }
            }
        }

        // Validity period
        if (bundle.ValidityEndDate <= bundle.ValidityStartDate)
            result.AddError("ValidityEndDate must be after ValidityStartDate");

        // Minimum order amount
        if (bundle.MinOrderAmount.HasValue)
        {
            if (bundle.MinOrderAmount.Value <= 0)
                result.AddError("MinOrderAmount must be greater than 0 when specified");

            if (string.IsNullOrWhiteSpace(bundle.MinOrderCurrency))
                result.AddError("MinOrderCurrency is required when MinOrderAmount is specified");
            else if (bundle.MinOrderCurrency.Length != 3)
                result.AddError("MinOrderCurrency must be a 3-letter code (e.g., VND, USD)");
        }

        // Usage limits
        if (bundle.TotalUsageLimit.HasValue && bundle.TotalUsageLimit.Value <= 0)
            result.AddError("TotalUsageLimit must be greater than 0 when specified");

        if (bundle.UsageLimitPerUser.HasValue && bundle.UsageLimitPerUser.Value <= 0)
            result.AddError("UsageLimitPerUser must be greater than 0 when specified");

        return result;
    }
}
