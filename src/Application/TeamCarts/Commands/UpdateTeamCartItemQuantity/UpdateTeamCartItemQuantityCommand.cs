using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;

[Authorize]
public sealed record UpdateTeamCartItemQuantityCommand(
    Guid TeamCartId,
    Guid TeamCartItemId,
    int NewQuantity
) : IRequest<Result<Unit>>;

public static class UpdateTeamCartItemQuantityErrors
{
    public static Error ItemNotFound(Guid itemId) => Error.NotFound(
        "UpdateTeamCartItemQuantity.ItemNotFound",
        $"TeamCart item '{itemId}' was not found.");

    public static Error NotItemOwner(Guid userId, Guid itemId) => Error.Validation(
        "UpdateTeamCartItemQuantity.NotOwner",
        $"User '{userId}' is not the owner of item '{itemId}'.");
}
