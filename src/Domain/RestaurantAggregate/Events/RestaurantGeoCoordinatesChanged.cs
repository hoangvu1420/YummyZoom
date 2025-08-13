using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate.Events;

public sealed record RestaurantGeoCoordinatesChanged(
    RestaurantId RestaurantId,
    GeoCoordinates? OldCoordinates,
    GeoCoordinates NewCoordinates
) : IDomainEvent;
