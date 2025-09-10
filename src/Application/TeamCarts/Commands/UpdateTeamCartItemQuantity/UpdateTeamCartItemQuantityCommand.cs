using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record UpdateTeamCartItemQuantityCommand(
    Guid TeamCartId,
    Guid TeamCartItemId,
    int NewQuantity
) : IRequest<Result>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public static class UpdateTeamCartItemQuantityErrors
{
    public static Error ItemNotFound(Guid itemId) => Error.NotFound(
        "UpdateTeamCartItemQuantity.ItemNotFound",
        $"TeamCart item '{itemId}' was not found.");

    public static Error NotItemOwner(Guid userId, Guid itemId) => Error.Validation(
        "UpdateTeamCartItemQuantity.NotOwner",
        $"User '{userId}' is not the owner of item '{itemId}'.");
}
