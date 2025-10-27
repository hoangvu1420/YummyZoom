using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;

[Authorize(Policy = Policies.MustBeTeamCartHost)]
public sealed record ConvertTeamCartToOrderCommand(
    Guid TeamCartId,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string? SpecialInstructions,
    string? IdempotencyKey = null,
    long? QuoteVersion = null
) : IRequest<Result<ConvertTeamCartToOrderResponse>>, ITeamCartCommand, IIdempotentCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public sealed record ConvertTeamCartToOrderResponse(
    Guid OrderId
);


