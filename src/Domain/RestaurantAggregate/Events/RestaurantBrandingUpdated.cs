using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantBrandingUpdated(
    RestaurantId RestaurantId,
    string OldName,
    string NewName,
    string? OldLogoUrl,
    string? NewLogoUrl,
    string OldDescription,
    string NewDescription) : IDomainEvent;
