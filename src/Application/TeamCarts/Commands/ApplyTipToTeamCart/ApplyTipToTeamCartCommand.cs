using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;

[Authorize]
public sealed record ApplyTipToTeamCartCommand(
    Guid TeamCartId,
    decimal TipAmount
) : IRequest<Result<Unit>>;

