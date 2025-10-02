using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.TeamCarts.Queries.Common;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;

/// <summary>
/// Retrieves detailed TeamCart information including members, items, payments and financial calculations.
/// Authorization: caller must be a member of the team cart (enforced post-fetch).
/// </summary>
public sealed record GetTeamCartDetailsQuery(Guid TeamCartIdGuid) : IRequest<Result<GetTeamCartDetailsResponse>>, ITeamCartQuery
{
    // Map primitive to ValueObject for contextual authorization interface.
    TeamCartId ITeamCartQuery.TeamCartId => TeamCartId.Create(TeamCartIdGuid);
}

public sealed record GetTeamCartDetailsResponse(TeamCartDetailsDto TeamCart);

/// <summary>
/// Errors specific to the GetTeamCartDetails query.
/// </summary>
public static class GetTeamCartDetailsErrors
{
    public static Error NotFound => Error.NotFound(
        "GetTeamCartDetails.NotFound", "The requested team cart was not found.");

    public static Error NotMember => Error.Validation(
        "GetTeamCartDetails.NotMember", "You are not a member of this team cart.");
}
