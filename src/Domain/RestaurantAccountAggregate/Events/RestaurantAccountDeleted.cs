using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Events;

public record RestaurantAccountDeleted(RestaurantAccountId RestaurantAccountId) : DomainEventBase;
