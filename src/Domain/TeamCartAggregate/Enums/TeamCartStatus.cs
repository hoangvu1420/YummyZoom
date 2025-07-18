namespace YummyZoom.Domain.TeamCartAggregate.Enums;

/// <summary>
/// Represents the current status of a team cart.
/// </summary>
public enum TeamCartStatus
{
    /// <summary>
    /// The team cart is open for members to join and add items.
    /// </summary>
    Open,

    /// <summary>
    /// Checkout has been initiated and the team cart is waiting for payments.
    /// </summary>
    AwaitingPayments,

    /// <summary>
    /// All payments are complete or committed and the team cart is ready to be converted to an order.
    /// </summary>
    ReadyToConfirm,

    /// <summary>
    /// The team cart has been successfully converted to an order.
    /// </summary>
    Converted,

    /// <summary>
    /// The team cart has expired due to timeout or manual expiration.
    /// </summary>
    Expired
}