using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

/// <summary>
/// Helper methods for creating test Coupon objects
/// </summary>
public static class CouponTestHelpers
{
    public static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    public const string DefaultCode = "SAVE10";
    public const string DefaultDescription = "Save 10% on your order";
    public static readonly CouponValue DefaultValue = CouponValue.CreatePercentage(10m).Value;
    public static readonly AppliesTo DefaultAppliesTo = AppliesTo.CreateForWholeOrder().Value;
    public static readonly DateTime DefaultStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultEndDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    public static readonly Money DefaultMinOrderAmount = new Money(25.00m, Currencies.Default);
    
    /// <summary>
    /// Creates a valid coupon for testing
    /// </summary>
    public static Coupon CreateValidCoupon(
        RestaurantId? restaurantId = null,
        string? code = null,
        string? description = null,
        CouponValue? value = null,
        AppliesTo? appliesTo = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Money? minOrderAmount = null,
        int? totalUsageLimit = null,
        int? usageLimitPerUser = null,
        bool isEnabled = true)
    {
        var result = Coupon.Create(
            restaurantId ?? DefaultRestaurantId,
            code ?? DefaultCode,
            description ?? DefaultDescription,
            value ?? DefaultValue,
            appliesTo ?? DefaultAppliesTo,
            startDate ?? DefaultStartDate,
            endDate ?? DefaultEndDate,
            minOrderAmount,
            totalUsageLimit,
            usageLimitPerUser,
            isEnabled);
            
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create test coupon: {result.Error}");
        }
        
        return result.Value;
    }
    
    /// <summary>
    /// Creates a percentage-based coupon
    /// </summary>
    public static Coupon CreatePercentageCoupon(decimal percentage = 10m, string code = "PERCENT10")
    {
        var value = CouponValue.CreatePercentage(percentage).Value;
        return CreateValidCoupon(code: code, value: value);
    }
    
    /// <summary>
    /// Creates a fixed amount coupon
    /// </summary>
    public static Coupon CreateFixedAmountCoupon(decimal amount = 5m, string code = "FIXED5")
    {
        var value = CouponValue.CreateFixedAmount(new Money(amount, Currencies.Default)).Value;
        return CreateValidCoupon(code: code, value: value);
    }
    
    /// <summary>
    /// Creates an expired coupon
    /// </summary>
    public static Coupon CreateExpiredCoupon(string code = "EXPIRED")
    {
        var pastStartDate = DateTime.UtcNow.AddDays(-30);
        var pastEndDate = DateTime.UtcNow.AddDays(-1);
        return CreateValidCoupon(
            code: code,
            startDate: pastStartDate,
            endDate: pastEndDate);
    }
    
    /// <summary>
    /// Creates a coupon that hasn't started yet
    /// </summary>
    public static Coupon CreateFutureCoupon(string code = "FUTURE")
    {
        var futureStartDate = DateTime.UtcNow.AddDays(1);
        var futureEndDate = DateTime.UtcNow.AddDays(30);
        return CreateValidCoupon(
            code: code,
            startDate: futureStartDate,
            endDate: futureEndDate);
    }
    
    /// <summary>
    /// Creates a disabled coupon
    /// </summary>
    public static Coupon CreateDisabledCoupon(string code = "DISABLED")
    {
        return CreateValidCoupon(code: code, isEnabled: false);
    }
    
    /// <summary>
    /// Creates a coupon with usage limits
    /// </summary>
    public static Coupon CreateCouponWithUsageLimits(
        int totalUsageLimit = 100,
        int usageLimitPerUser = 5,
        string code = "LIMITED")
    {
        return CreateValidCoupon(
            code: code,
            totalUsageLimit: totalUsageLimit,
            usageLimitPerUser: usageLimitPerUser);
    }
    
    /// <summary>
    /// Creates a coupon with minimum order amount
    /// </summary>
    public static Coupon CreateCouponWithMinOrder(
        decimal minOrderAmount = 50m,
        string code = "MINORDER")
    {
        var minOrder = new Money(minOrderAmount, Currencies.Default);
        return CreateValidCoupon(
            code: code,
            minOrderAmount: minOrder);
    }
}
