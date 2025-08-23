using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

/// <summary>
/// Domain event raised when an order enters the Placed lifecycle state.
/// Operational visibility event (not a financial recognition event).
/// </summary>
public record OrderPlaced(OrderId OrderId) : DomainEventBase;
