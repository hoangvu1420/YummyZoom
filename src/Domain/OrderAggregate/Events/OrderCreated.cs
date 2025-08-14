using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderCreated(
    OrderId OrderId,
    UserId CustomerId,
    RestaurantId RestaurantId,
    Money TotalAmount) : DomainEventBase;
