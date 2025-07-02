using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderPreparing(OrderId OrderId) : IDomainEvent;
