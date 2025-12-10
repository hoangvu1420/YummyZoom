using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetActiveTeamCart;

public sealed record GetActiveTeamCartQuery : IRequest<Result<GetActiveTeamCartResponse?>>;

public sealed record GetActiveTeamCartResponse(
    Guid TeamCartId,
    Guid RestaurantId,
    string RestaurantName,
    string? RestaurantImage,
    string State,
    int TotalItemCount,
    decimal MyShareTotal,
    string Currency,
    bool IsHost,
    DateTime CreatedAt
);
