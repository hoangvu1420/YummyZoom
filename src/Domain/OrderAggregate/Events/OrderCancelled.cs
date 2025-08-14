using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderCancelled(OrderId OrderId) : DomainEventBase;
