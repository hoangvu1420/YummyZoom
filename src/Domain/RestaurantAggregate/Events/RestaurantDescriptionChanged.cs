using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantDescriptionChanged(
    RestaurantId RestaurantId,
    string OldDescription,
    string NewDescription) : IDomainEvent;
