using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

/// <summary>
/// Domain event raised when an order payment fails.
/// </summary>
public record OrderPaymentFailed(
    OrderId OrderId) : DomainEventBase;
