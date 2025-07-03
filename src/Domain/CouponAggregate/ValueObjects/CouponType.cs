namespace YummyZoom.Domain.CouponAggregate.ValueObjects;

/// <summary>
/// Defines the type of discount provided by a coupon
/// </summary>
public enum CouponType
{
    /// <summary>
    /// Percentage discount (e.g., 10% off)
    /// </summary>
    Percentage = 1,
    
    /// <summary>
    /// Fixed amount discount (e.g., $5 off)
    /// </summary>
    FixedAmount = 2,
    
    /// <summary>
    /// Free menu item (e.g., free appetizer)
    /// </summary>
    FreeItem = 3
}
