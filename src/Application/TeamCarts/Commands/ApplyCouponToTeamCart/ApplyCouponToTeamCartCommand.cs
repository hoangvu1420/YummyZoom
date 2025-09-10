using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;

[Authorize(Policy = Policies.MustBeTeamCartHost)]
public sealed record ApplyCouponToTeamCartCommand(
    Guid TeamCartId,
    string CouponCode
) : IRequest<Result>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public static class ApplyCouponToTeamCartErrors
{
    public static Error CouponNotFound(string couponCode) =>
        Error.NotFound("Coupon.CouponNotFound", $"The specified coupon code {couponCode} is not valid.");
}

