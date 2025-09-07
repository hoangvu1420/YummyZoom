using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;

[Authorize]
public sealed record JoinTeamCartCommand(
    Guid TeamCartId,
    string ShareToken,
    string GuestName
) : IRequest<Result<Unit>>;

