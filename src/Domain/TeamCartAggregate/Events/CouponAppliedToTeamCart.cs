using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a coupon is applied to a TeamCart.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="CouponId">The applied coupon id.</param>
public sealed record CouponAppliedToTeamCart(
    TeamCartId TeamCartId,
    CouponId CouponId) : DomainEventBase;

