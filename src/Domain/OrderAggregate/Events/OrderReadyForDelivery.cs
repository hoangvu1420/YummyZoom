using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderReadyForDelivery(OrderId OrderId) : DomainEventBase;
