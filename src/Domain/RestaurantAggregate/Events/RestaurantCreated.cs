
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantCreated(RestaurantId RestaurantId) : IDomainEvent;
