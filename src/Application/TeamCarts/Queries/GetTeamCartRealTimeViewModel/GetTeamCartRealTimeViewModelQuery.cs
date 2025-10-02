using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;

/// <summary>
/// Retrieves the real-time TeamCart view model from Redis cache.
/// This query is optimized for live updates and never hits SQL.
/// Authorization: caller must be a member of the team cart (enforced post-fetch).
/// </summary>
public sealed record GetTeamCartRealTimeViewModelQuery(Guid TeamCartIdGuid) : IRequest<Result<GetTeamCartRealTimeViewModelResponse>>, ITeamCartQuery
{
    // Map primitive to ValueObject for contextual authorization interface.
    TeamCartId ITeamCartQuery.TeamCartId => TeamCartId.Create(TeamCartIdGuid);
}

public sealed record GetTeamCartRealTimeViewModelResponse(TeamCartViewModel TeamCart);

/// <summary>
/// Errors specific to the GetTeamCartRealTimeViewModel query.
/// </summary>
public static class GetTeamCartRealTimeViewModelErrors
{
    public static Error NotFound => Error.NotFound(
        "GetTeamCartRealTimeViewModel.NotFound", "The requested team cart real-time view was not found.");

    public static Error NotMember => Error.Validation(
        "GetTeamCartRealTimeViewModel.NotMember", "You are not a member of this team cart.");
}
