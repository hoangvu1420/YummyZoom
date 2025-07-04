using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public record RestaurantContactInfoChanged(
    RestaurantId RestaurantId,
    ContactInfo OldContactInfo,
    ContactInfo NewContactInfo) : IDomainEvent;
