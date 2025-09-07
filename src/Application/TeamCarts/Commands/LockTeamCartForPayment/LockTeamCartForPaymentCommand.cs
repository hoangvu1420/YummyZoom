using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

[Authorize]
public sealed record LockTeamCartForPaymentCommand(
    Guid TeamCartId
) : IRequest<Result<Unit>>;

