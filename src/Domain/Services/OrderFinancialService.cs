using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Services;

/// <summary>
/// A stateless domain service for handling all order-related financial calculations.
/// </summary>
public class OrderFinancialService
{
    /// <summary>
    /// Calculates the pre-discount, pre-tax, pre-fee subtotal of an order.
    /// </summary>
    public Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)
    {
        if (!orderItems.Any())
        {
            // Assuming a default currency can be determined, or throw an exception if currency is ambiguous.
            return Money.Zero("USD");
        }
        var currency = orderItems.First().LineItemTotal.Currency;
        return orderItems.Sum(item => item.LineItemTotal, currency);
    }

    /// <summary>
    /// Validates a coupon's rules and calculates the resulting discount amount.
    /// This method is pure and receives all necessary data.
    /// </summary>
    /// <param name="coupon">The coupon to validate and apply.</param>
    /// <param name="currentUserUsageCount">The number of times the current user has used this coupon.</param>
    /// <param name="orderItems">The items in the order to which the coupon will be applied.</param>
    /// <param name="subtotal">The subtotal of the order before discounts.</param>
    /// <param name="currentTime">The current time to use for validation (defaults to DateTime.UtcNow if not provided).</param>
    /// <returns>A Result containing the calculated discount amount if successful.</returns>
    public Result<Money> ValidateAndCalculateDiscount(
        Coupon coupon,
        int currentUserUsageCount,
        IReadOnlyList<OrderItem> orderItems,
        Money subtotal,
        DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        
        // 1. Basic Validity Checks
        if (!coupon.IsEnabled) 
            return Result.Failure<Money>(CouponErrors.CouponDisabled);
        if (now < coupon.ValidityStartDate) 
            return Result.Failure<Money>(CouponErrors.CouponNotYetValid);
        if (now > coupon.ValidityEndDate) 
            return Result.Failure<Money>(CouponErrors.CouponExpired);

        // 2. Usage Limit Checks
        if (coupon.TotalUsageLimit.HasValue && coupon.CurrentTotalUsageCount >= coupon.TotalUsageLimit.Value)
            return Result.Failure<Money>(CouponErrors.UsageLimitExceeded);
        if (coupon.UsageLimitPerUser.HasValue && currentUserUsageCount >= coupon.UsageLimitPerUser.Value)
            return Result.Failure<Money>(CouponErrors.UserUsageLimitExceeded);

        // 3. Order Condition Checks
        if (coupon.MinOrderAmount is not null && subtotal.Amount < coupon.MinOrderAmount.Amount)
            return Result.Failure<Money>(CouponErrors.MinAmountNotMet);

        // 4. Calculate Discount Base
        decimal discountBaseAmount = coupon.AppliesTo.Scope switch
        {
            CouponScope.WholeOrder => subtotal.Amount,
            CouponScope.SpecificItems => orderItems
                .Where(oi => coupon.AppliesTo.ItemIds.Contains(oi.Snapshot_MenuItemId))
                .Sum(oi => oi.LineItemTotal.Amount),
            CouponScope.SpecificCategories => orderItems
                .Where(oi => coupon.AppliesTo.CategoryIds.Contains(oi.Snapshot_MenuCategoryId))
                .Sum(oi => oi.LineItemTotal.Amount),
            _ => 0m
        };

        if (discountBaseAmount <= 0) return Result.Failure<Money>(CouponErrors.NotApplicable);

        // 5. Calculate Final Discount Value
        Money calculatedDiscount;
        switch (coupon.Value.Type)
        {
            case CouponType.Percentage:
                calculatedDiscount = new Money(discountBaseAmount * (coupon.Value.PercentageValue!.Value / 100m), subtotal.Currency);
                break;
            case CouponType.FixedAmount:
                var fixedAmount = coupon.Value.FixedAmountValue!.Amount;
                calculatedDiscount = new Money(Math.Min(discountBaseAmount, fixedAmount), subtotal.Currency);
                break;
            case CouponType.FreeItem:
                var freeItem = orderItems
                    .Where(oi => oi.Snapshot_MenuItemId == coupon.Value.FreeItemValue!)
                    .OrderBy(oi => oi.LineItemTotal.Amount / oi.Quantity) // Price per unit
                    .FirstOrDefault();
                if (freeItem is null) return Result.Failure<Money>(CouponErrors.NotApplicable);
                calculatedDiscount = new Money(freeItem.LineItemTotal.Amount / freeItem.Quantity, subtotal.Currency);
                break;
            default:
                return Result.Failure<Money>(CouponErrors.InvalidType);
        }
        
        // Ensure discount doesn't exceed the subtotal it applies to
        return new Money(Math.Min(calculatedDiscount.Amount, discountBaseAmount), subtotal.Currency);
    }

    /// <summary>
    /// Calculates the final total amount to be charged.
    /// </summary>
    public Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)
    {
        var finalAmount = subtotal - discount + deliveryFee + tip + tax;
        // Ensure total is not negative
        if (finalAmount.Amount < 0)
        {
            return Money.Zero(finalAmount.Currency);
        }
        return finalAmount;
    }
}
