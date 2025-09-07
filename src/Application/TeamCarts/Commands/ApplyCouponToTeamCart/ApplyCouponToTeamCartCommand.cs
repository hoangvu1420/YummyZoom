using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;

[Authorize]
public sealed record ApplyCouponToTeamCartCommand(
    Guid TeamCartId,
    string CouponCode
) : IRequest<Result<Unit>>;

public static class ApplyCouponToTeamCartErrors
{
    public static Error CouponNotFound(string couponCode) =>
        Error.NotFound("Coupon.CouponNotFound", $"The specified coupon code {couponCode} is not valid.");
}

