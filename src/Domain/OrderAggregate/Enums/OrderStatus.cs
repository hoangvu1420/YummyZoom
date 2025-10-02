namespace YummyZoom.Domain.OrderAggregate.Enums;

/// <summary>
/// Represents the current status of an order in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// The order has been created but is awaiting successful payment 
    /// before being sent to the restaurant. It is not yet actionable.
    /// </summary>
    AwaitingPayment,

    /// <summary>
    /// Order has been successfully placed (payment confirmed or COD) 
    /// and is ready for restaurant review.
    /// </summary>
    Placed,

    /// <summary>
    /// Order has been accepted by the restaurant.
    /// </summary>
    Accepted,

    /// <summary>
    /// Order is being prepared by the restaurant.
    /// </summary>
    Preparing,

    /// <summary>
    /// Order is ready for delivery.
    /// </summary>
    ReadyForDelivery,

    /// <summary>
    /// Order has been delivered to the customer.
    /// </summary>
    Delivered,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Order has been rejected by the restaurant.
    /// </summary>
    Rejected
}
