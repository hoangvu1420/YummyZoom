namespace YummyZoom.Domain.OrderAggregate.Enums;

public enum OrderStatus
{
    Placed,
    Accepted,
    Preparing,
    ReadyForDelivery,
    Delivered,
    Cancelled,
    Rejected
}
