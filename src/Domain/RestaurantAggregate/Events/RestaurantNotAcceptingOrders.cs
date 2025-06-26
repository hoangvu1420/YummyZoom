
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantNotAcceptingOrders(RestaurantId RestaurantId) : IDomainEvent;
