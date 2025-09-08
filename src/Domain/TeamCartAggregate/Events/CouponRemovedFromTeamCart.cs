using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a coupon is removed from a TeamCart.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
public sealed record CouponRemovedFromTeamCart(
    TeamCartId TeamCartId) : DomainEventBase;

