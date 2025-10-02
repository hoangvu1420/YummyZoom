using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;

[Authorize]
public sealed record CreateTeamCartCommand(
    Guid RestaurantId,
    string HostName,
    DateTime? DeadlineUtc = null
) : IRequest<Result<CreateTeamCartResponse>>;

public sealed record CreateTeamCartResponse(
    Guid TeamCartId,
    string ShareToken,
    DateTime ShareTokenExpiresAtUtc
);

public static class CreateTeamCartErrors
{
    public static Error RestaurantNotFound(Guid restaurantId) => Error.NotFound(
        "CreateTeamCart.RestaurantNotFound",
        "Restaurant not found");
}
