using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantCuisineTypeChanged(
    RestaurantId RestaurantId,
    string OldCuisineType,
    string NewCuisineType) : IDomainEvent;
