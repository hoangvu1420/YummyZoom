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
    public virtual Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)
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
    /// Validates a coupon's business rules and calculates the resulting discount amount.
    /// This method performs all validations except total usage limit checks, which are handled
    /// atomically at the repository level to prevent race conditions.
    /// </summary>
    /// <param name="coupon">The coupon to validate and apply.</param>
    /// <param name="orderItems">The items in the order to which the coupon will be applied.</param>
    /// <param name="subtotal">The subtotal of the order before discounts.</param>
    /// <param name="currentTime">The current time to use for validation (defaults to DateTime.UtcNow if not provided).</param>
    /// <returns>A Result containing the calculated discount amount if successful.</returns>
    public virtual Result<Money> ValidateAndCalculateDiscount(
        Coupon coupon,
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
        // Note: Total and per-user usage limit checks are handled atomically at the repository level
        // to prevent race conditions in concurrent scenarios. This service does not validate usage counts.

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
                var discountAmount = discountBaseAmount * (coupon.Value.PercentageValue!.Value / 100m);
                calculatedDiscount = new Money(discountAmount, subtotal.Currency);
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
    /// Uses the subtotal's currency for the final result when dealing with mixed currencies.
    /// </summary>
    public virtual Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)
    {
        // Convert all amounts to use the subtotal's currency to handle mixed currency scenarios
        var discountInSubtotalCurrency = new Money(discount.Amount, subtotal.Currency);
        var deliveryFeeInSubtotalCurrency = new Money(deliveryFee.Amount, subtotal.Currency);
        var tipInSubtotalCurrency = new Money(tip.Amount, subtotal.Currency);
        var taxInSubtotalCurrency = new Money(tax.Amount, subtotal.Currency);
        
        var finalAmount = subtotal - discountInSubtotalCurrency + deliveryFeeInSubtotalCurrency + tipInSubtotalCurrency + taxInSubtotalCurrency;
        
        // Ensure total is not negative
        if (finalAmount.Amount < 0)
        {
            return Money.Zero(subtotal.Currency);
        }
        return finalAmount;
    }
}
