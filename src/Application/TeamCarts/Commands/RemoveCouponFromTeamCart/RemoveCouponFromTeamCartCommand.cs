using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;

[Authorize]
public sealed record RemoveCouponFromTeamCartCommand(
    Guid TeamCartId
) : IRequest<Result<Unit>>;

