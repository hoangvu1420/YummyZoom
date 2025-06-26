
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantUpdated(RestaurantId RestaurantId) : IDomainEvent;
