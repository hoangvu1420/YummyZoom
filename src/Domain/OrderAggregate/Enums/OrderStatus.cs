namespace YummyZoom.Domain.OrderAggregate.Enums;

/// <summary>
/// Represents the current status of an order in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is awaiting payment confirmation from the payment provider.
    /// This is the initial state for orders with online payment.
    /// </summary>
    PendingPayment,
    
    /// <summary>
    /// Order has been placed and payment has been confirmed.
    /// This is the initial state for cash-on-delivery orders.
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
    Rejected,
    
    /// <summary>
    /// Payment for the order has failed.
    /// </summary>
    PaymentFailed
}
