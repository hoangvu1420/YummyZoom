namespace YummyZoom.Domain.TeamCartAggregate.Enums;

/// <summary>
/// Represents the current status of a team cart.
/// </summary>
public enum TeamCartStatus
{
    /// <summary>
    /// The team cart is open for members to join and add items.
    /// This is the initial "Wild West" phase where the cart is fluid.
    /// </summary>
    Open,

    /// <summary>
    /// The cart is locked for payment. Item-related changes are forbidden.
    /// Host is reviewing totals and can apply tip/coupon before finalizing.
    /// Members wait for pricing to be finalized before they can pay.
    /// </summary>
    Locked,

    /// <summary>
    /// Pricing is finalized. Tip and coupon are now immutable.
    /// Members can now commit payments with confidence that amounts won't change.
    /// This is the "Settle Up" phase where members pay their share.
    /// </summary>
    Finalized,

    /// <summary>
    /// All payments are complete or committed and the team cart is ready to be converted to an order.
    /// The cart is now immutable and ready for the final conversion step.
    /// </summary>
    ReadyToConfirm,

    /// <summary>
    /// The team cart has been successfully converted to an order.
    /// This is a terminal state.
    /// </summary>
    Converted,

    /// <summary>
    /// The team cart has expired due to timeout or manual expiration.
    /// This is also a terminal state.
    /// </summary>
    Expired
}
