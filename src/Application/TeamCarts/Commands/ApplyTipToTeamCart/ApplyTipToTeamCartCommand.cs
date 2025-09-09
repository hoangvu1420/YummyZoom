using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;

[Authorize(Policy = Policies.MustBeTeamCartParticipant)]
public sealed record ApplyTipToTeamCartCommand(
    Guid TeamCartId,
    decimal TipAmount
) : IRequest<Result<Unit>>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

