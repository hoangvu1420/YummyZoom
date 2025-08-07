using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.TestData;

/// <summary>
/// Factory for creating various coupon test scenarios with a unified interface.
/// Consolidates all coupon creation logic for better maintainability and reusability.
/// </summary>
public static class CouponTestDataFactory
{
    /// <summary>
    /// Creates a test coupon with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the coupon.</param>
    /// <returns>The coupon code of the created coupon.</returns>
    public static async Task<string> CreateTestCouponAsync(CouponTestOptions options)
    {
        var restaurantId = RestaurantId.Create(options.RestaurantId ?? TestDataFactory.DefaultRestaurantId);

        // Create coupon value (percentage or fixed amount)
        var couponValueResult = options.FixedDiscountAmount.HasValue
            ? CouponValue.CreateFixedAmount(new Money(options.FixedDiscountAmount.Value, options.Currency))
            : CouponValue.CreatePercentage(options.DiscountPercentage);

        if (couponValueResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon value: {couponValueResult.Error}");

        // Create applies to (whole order, specific items, or categories)
        var appliesToResult = options.SpecificMenuItemId.HasValue
            ? AppliesTo.CreateForSpecificItems(new List<MenuItemId> { MenuItemId.Create(options.SpecificMenuItemId.Value) })
            : AppliesTo.CreateForWholeOrder();

        if (appliesToResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon applies to: {appliesToResult.Error}");

        // Determine dates based on options
        var (startDate, endDate) = GetCouponDates(options);

        // Create minimum order amount if specified
        Money? minimumOrderAmount = options.MinimumOrderAmount.HasValue
            ? new Money(options.MinimumOrderAmount.Value, options.Currency)
            : null;

        // Generate unique code if not provided
        var code = options.Code ?? GenerateCouponCode(options);

        // Create the coupon
        var couponResult = Coupon.Create(
            restaurantId,
            code,
            options.Description ?? GetDefaultDescription(options),
            couponValueResult.Value,
            appliesToResult.Value,
            startDate,
            endDate,
            minimumOrderAmount,
            totalUsageLimit: options.TotalUsageLimit ?? 100,
            usageLimitPerUser: options.UserUsageLimit ?? 10,
            isEnabled: !options.IsDisabled);

        if (couponResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon: {couponResult.Error}");

        var coupon = couponResult.Value;
        await AddAsync(coupon);

        return code;
    }

    /// <summary>
    /// Creates an expired coupon for testing expiry validation.
    /// </summary>
    public static Task<string> CreateExpiredCouponAsync() =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "EXPIRED10",
            IsExpired = true,
            Description = "Expired test coupon"
        });

    /// <summary>
    /// Creates a disabled coupon for testing disabled state validation.
    /// </summary>
    public static Task<string> CreateDisabledCouponAsync() =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "DISABLED10",
            IsDisabled = true,
            Description = "Disabled test coupon"
        });

    /// <summary>
    /// Creates a coupon with a specific total usage limit.
    /// </summary>
    public static Task<string> CreateCouponWithUsageLimitAsync(int totalLimit) =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = $"LIMITED{totalLimit}",
            TotalUsageLimit = totalLimit,
            Description = $"Limited usage test coupon - {totalLimit} uses"
        });

    /// <summary>
    /// Creates a coupon with a specific per-user usage limit.
    /// </summary>
    public static Task<string> CreateCouponWithUserUsageLimitAsync(int userLimit) =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = $"USERLIMIT{userLimit}",
            UserUsageLimit = userLimit,
            TotalUsageLimit = 1000, // High total limit to focus on user limit
            Description = $"User limited test coupon - {userLimit} per user"
        });

    /// <summary>
    /// Creates a coupon with a minimum order amount requirement.
    /// </summary>
    public static Task<string> CreateCouponWithMinimumOrderAmountAsync(decimal minimumAmount) =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = $"MINORDER{(int)minimumAmount}",
            MinimumOrderAmount = minimumAmount,
            Description = $"Minimum order test coupon - ${minimumAmount}"
        });

    /// <summary>
    /// Creates a coupon that applies only to a specific menu item.
    /// </summary>
    public static Task<string> CreateCouponForSpecificItemAsync(Guid menuItemId) =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "SPECIFIC10",
            SpecificMenuItemId = menuItemId,
            Description = "Specific item test coupon"
        });

    /// <summary>
    /// Creates a coupon that is not yet valid (starts in the future).
    /// </summary>
    public static Task<string> CreateFutureCouponAsync() =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "FUTURE10",
            IsInFuture = true,
            Description = "Future test coupon"
        });

    /// <summary>
    /// Creates a coupon for a different restaurant.
    /// </summary>
    public static Task<string> CreateCouponForRestaurantAsync(Guid restaurantId) =>
        CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "OTHERREST10",
            RestaurantId = restaurantId,
            Description = "Other restaurant test coupon"
        });

    #region Private Helper Methods

    private static (DateTime startDate, DateTime endDate) GetCouponDates(CouponTestOptions options)
    {
        if (options.IsExpired)
        {
            return (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1));
        }

        if (options.IsInFuture)
        {
            return (DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30));
        }

        // Default: valid period
        return (DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
    }

    private static string GenerateCouponCode(CouponTestOptions options)
    {
        if (options.IsExpired) return "EXPIRED10";
        if (options.IsDisabled) return "DISABLED10";
        if (options.IsInFuture) return "FUTURE10";
        if (options.TotalUsageLimit.HasValue) return $"LIMITED{options.TotalUsageLimit}";
        if (options.UserUsageLimit.HasValue) return $"USERLIMIT{options.UserUsageLimit}";
        if (options.MinimumOrderAmount.HasValue) return $"MINORDER{(int)options.MinimumOrderAmount}";
        if (options.SpecificMenuItemId.HasValue) return "SPECIFIC10";
        if (options.RestaurantId.HasValue) return "OTHERREST10";

        return $"TEST{DateTime.UtcNow.Ticks % 10000}";
    }

    private static string GetDefaultDescription(CouponTestOptions options)
    {
        if (options.IsExpired) return "Expired test coupon";
        if (options.IsDisabled) return "Disabled test coupon";
        if (options.IsInFuture) return "Future test coupon";
        if (options.TotalUsageLimit.HasValue) return $"Limited usage test coupon - {options.TotalUsageLimit} uses";
        if (options.UserUsageLimit.HasValue) return $"User limited test coupon - {options.UserUsageLimit} per user";
        if (options.MinimumOrderAmount.HasValue) return $"Minimum order test coupon - ${options.MinimumOrderAmount}";
        if (options.SpecificMenuItemId.HasValue) return "Specific item test coupon";
        if (options.RestaurantId.HasValue) return "Other restaurant test coupon";

        return "Test coupon";
    }

    #endregion
}

/// <summary>
/// Configuration options for creating test coupons.
/// Allows for flexible combination of different coupon scenarios.
/// </summary>
public class CouponTestOptions
{
    /// <summary>
    /// The coupon code. If not specified, will be auto-generated based on other options.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The description of the coupon. If not specified, will be auto-generated.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the coupon should be expired (end date in the past).
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether the coupon should be disabled.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Whether the coupon should be in the future (start date in the future).
    /// </summary>
    public bool IsInFuture { get; set; }

    /// <summary>
    /// The total usage limit for the coupon. Defaults to 100 if not specified.
    /// </summary>
    public int? TotalUsageLimit { get; set; }

    /// <summary>
    /// The per-user usage limit for the coupon. Defaults to 10 if not specified.
    /// </summary>
    public int? UserUsageLimit { get; set; }

    /// <summary>
    /// The minimum order amount required to use the coupon.
    /// </summary>
    public decimal? MinimumOrderAmount { get; set; }

    /// <summary>
    /// The specific menu item ID this coupon applies to. If not specified, applies to whole order.
    /// </summary>
    public Guid? SpecificMenuItemId { get; set; }

    /// <summary>
    /// The restaurant ID this coupon belongs to. Defaults to the default test restaurant.
    /// </summary>
    public Guid? RestaurantId { get; set; }

    /// <summary>
    /// The discount percentage. Defaults to 10%.
    /// </summary>
    public decimal DiscountPercentage { get; set; } = 10m;

    /// <summary>
    /// If specified, creates a fixed amount discount instead of percentage.
    /// </summary>
    public decimal? FixedDiscountAmount { get; set; }

    /// <summary>
    /// The currency for monetary values. Defaults to USD.
    /// </summary>
    public string Currency { get; set; } = "USD";
}
