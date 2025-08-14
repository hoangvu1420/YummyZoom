using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderRejected(OrderId OrderId) : DomainEventBase;
