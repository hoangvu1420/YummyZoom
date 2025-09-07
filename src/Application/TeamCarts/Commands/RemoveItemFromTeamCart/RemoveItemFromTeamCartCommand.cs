using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;

[Authorize]
public sealed record RemoveItemFromTeamCartCommand(
    Guid TeamCartId,
    Guid TeamCartItemId
) : IRequest<Result<Unit>>;

public static class RemoveItemFromTeamCartErrors
{
    public static Error ItemNotFound(Guid itemId) => Error.NotFound(
        "RemoveItemFromTeamCart.ItemNotFound",
        $"TeamCart item '{itemId}' was not found.");

    public static Error NotItemOwner(Guid userId, Guid itemId) => Error.Validation(
        "RemoveItemFromTeamCart.NotOwner",
        $"User '{userId}' is not the owner of item '{itemId}'.");
}

