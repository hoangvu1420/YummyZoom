using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record RestaurantAccountCreated(
    RestaurantAccountId RestaurantAccountId, 
    RestaurantId RestaurantId) : IDomainEvent;
