using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record CommitToCodPaymentCommand(
    Guid TeamCartId
) : IRequest<Result<Unit>>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}


