using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantProfileUpdated(
    RestaurantId RestaurantId,
    string OldName,
    string NewName,
    string OldDescription,
    string NewDescription,
    string OldCuisineType,
    string NewCuisineType,
    string? OldLogoUrl,
    string? NewLogoUrl,
    Address OldLocation,
    Address NewLocation,
    ContactInfo OldContactInfo,
    ContactInfo NewContactInfo,
    BusinessHours OldBusinessHours,
    BusinessHours NewBusinessHours) : IDomainEvent;
