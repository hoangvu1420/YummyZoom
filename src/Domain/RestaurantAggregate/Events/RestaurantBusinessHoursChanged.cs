using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantBusinessHoursChanged(
    RestaurantId RestaurantId,
    BusinessHours OldBusinessHours,
    BusinessHours NewBusinessHours) : IDomainEvent;
