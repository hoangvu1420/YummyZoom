using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

/// <summary>
/// Domain event raised when an order payment is successfully confirmed.
/// </summary>
public record OrderPaymentSucceeded(
    OrderId OrderId) : DomainEventBase;
