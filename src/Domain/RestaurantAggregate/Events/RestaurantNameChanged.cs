using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantNameChanged(
    RestaurantId RestaurantId,
    string OldName,
    string NewName) : DomainEventBase;
