using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetCouponSuggestions;

/// <summary>
/// Gets coupon suggestions for a TeamCart based on current items.
/// Authorization: caller must be a member of the team cart (enforced post-fetch).
/// </summary>
public sealed record TeamCartCouponSuggestionsQuery(
    Guid TeamCartId
) : IRequest<Result<CouponSuggestionsResponse>>, ITeamCartQuery
{
    TeamCartId ITeamCartQuery.TeamCartId { get; } = Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}
