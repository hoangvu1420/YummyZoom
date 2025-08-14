using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantLocationChanged(
    RestaurantId RestaurantId,
    Address OldLocation,
    Address NewLocation) : DomainEventBase;
