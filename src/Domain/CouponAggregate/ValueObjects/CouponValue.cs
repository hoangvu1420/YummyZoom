using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CouponAggregate.ValueObjects;

/// <summary>
/// Represents the value of a coupon, which can be a percentage, fixed amount, or free item
/// </summary>
public sealed class CouponValue : ValueObject
{
    public CouponType Type { get; private set; }
    public decimal? PercentageValue { get; private set; }
    public Money? FixedAmountValue { get; private set; }
    public MenuItemId? FreeItemValue { get; private set; }

    private CouponValue(CouponType type, decimal? percentageValue, Money? fixedAmountValue, MenuItemId? freeItemValue)
    {
        Type = type;
        PercentageValue = percentageValue;
        FixedAmountValue = fixedAmountValue;
        FreeItemValue = freeItemValue;
    }

    /// <summary>
    /// Creates a percentage-based coupon value
    /// </summary>
    /// <param name="percentage">Percentage value (e.g., 10 for 10%)</param>
    public static Result<CouponValue> CreatePercentage(decimal percentage)
    {
        if (percentage <= 0)
        {
            return Result.Failure<CouponValue>(Error.Validation(
                "CouponValue.InvalidPercentage", 
                "Percentage must be greater than 0"));
        }

        if (percentage > 100)
        {
            return Result.Failure<CouponValue>(Error.Validation(
                "CouponValue.InvalidPercentage", 
                "Percentage cannot exceed 100"));
        }

        return Result.Success(new CouponValue(CouponType.Percentage, percentage, null, null));
    }

    /// <summary>
    /// Creates a fixed amount coupon value
    /// </summary>
    /// <param name="amount">Fixed monetary amount</param>
    public static Result<CouponValue> CreateFixedAmount(Money amount)
    {
        if (amount.Amount <= 0)
        {
            return Result.Failure<CouponValue>(Error.Validation(
                "CouponValue.InvalidAmount", 
                "Fixed amount must be greater than 0"));
        }

        return Result.Success(new CouponValue(CouponType.FixedAmount, null, amount, null));
    }

    /// <summary>
    /// Creates a free item coupon value
    /// </summary>
    /// <param name="menuItemId">ID of the free menu item</param>
    public static Result<CouponValue> CreateFreeItem(MenuItemId menuItemId)
    {
        return Result.Success(new CouponValue(CouponType.FreeItem, null, null, menuItemId));
    }

    /// <summary>
    /// Gets the display value as a string
    /// </summary>
    public string GetDisplayValue()
    {
        return Type switch
        {
            CouponType.Percentage => $"{PercentageValue}% off",
            CouponType.FixedAmount => $"{FixedAmountValue} off",
            CouponType.FreeItem => $"Free item (ID: {FreeItemValue?.Value})",
            _ => "Unknown coupon value"
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return PercentageValue ?? 0m;
        yield return FixedAmountValue ?? Money.Zero;
        yield return FreeItemValue?.Value ?? Guid.Empty;
    }

#pragma warning disable CS8618
    private CouponValue() { }
#pragma warning restore CS8618
}
