using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record RemoveItemFromTeamCartCommand(
    Guid TeamCartId,
    Guid TeamCartItemId
) : IRequest<Result>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public static class RemoveItemFromTeamCartErrors
{
    public static Error ItemNotFound(Guid itemId) => Error.NotFound(
        "RemoveItemFromTeamCart.ItemNotFound",
        $"TeamCart item '{itemId}' was not found.");

    public static Error NotItemOwner(Guid userId, Guid itemId) => Error.Validation(
        "RemoveItemFromTeamCart.NotOwner",
        $"User '{userId}' is not the owner of item '{itemId}'.");
}

