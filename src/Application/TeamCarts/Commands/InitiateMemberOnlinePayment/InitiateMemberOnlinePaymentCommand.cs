using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record InitiateMemberOnlinePaymentCommand(
    Guid TeamCartId
) : IRequest<Result<InitiateMemberOnlinePaymentResponse>>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public sealed record InitiateMemberOnlinePaymentResponse(
    string PaymentIntentId,
    string ClientSecret
);
