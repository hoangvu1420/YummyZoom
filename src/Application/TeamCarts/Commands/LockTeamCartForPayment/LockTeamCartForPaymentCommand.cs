using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

[Authorize(Policy = Policies.MustBeTeamCartHost)]
public sealed record LockTeamCartForPaymentCommand(
    Guid TeamCartId
) : IRequest<Result<Unit>>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

