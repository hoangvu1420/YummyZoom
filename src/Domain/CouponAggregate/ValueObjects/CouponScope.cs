namespace YummyZoom.Domain.CouponAggregate.ValueObjects;

/// <summary>
/// Defines the scope of items a coupon applies to
/// </summary>
public enum CouponScope
{
    /// <summary>
    /// Coupon applies to the entire order
    /// </summary>
    WholeOrder = 1,

    /// <summary>
    /// Coupon applies only to specific menu items
    /// </summary>
    SpecificItems = 2,

    /// <summary>
    /// Coupon applies only to specific menu categories
    /// </summary>
    SpecificCategories = 3
}
