using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

[Authorize(Policy = Policies.MustBeTeamCartHost)]
public sealed record LockTeamCartForPaymentCommand(
    Guid TeamCartId,
    string? IdempotencyKey = null
) : IRequest<Result<LockTeamCartForPaymentResponse>>, ITeamCartCommand, IIdempotentCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public sealed record LockTeamCartForPaymentResponse(
    long QuoteVersion
);

