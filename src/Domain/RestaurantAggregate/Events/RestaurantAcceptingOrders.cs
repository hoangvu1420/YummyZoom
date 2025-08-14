
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantAcceptingOrders(RestaurantId RestaurantId) : DomainEventBase;
