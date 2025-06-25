
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantNotAcceptingOrders(RestaurantId RestaurantId) : IDomainEvent;
