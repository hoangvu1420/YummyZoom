using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a new team cart is created.
/// </summary>
public sealed record TeamCartCreated(
    TeamCartId TeamCartId,
    UserId HostId,
    RestaurantId RestaurantId) : DomainEventBase;
