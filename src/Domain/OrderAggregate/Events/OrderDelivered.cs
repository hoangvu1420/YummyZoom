using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderDelivered(OrderId OrderId, DateTime DeliveredAt) : IDomainEvent;
