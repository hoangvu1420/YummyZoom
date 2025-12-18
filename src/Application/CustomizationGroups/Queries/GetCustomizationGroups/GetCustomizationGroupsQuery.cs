using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroups;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetCustomizationGroupsQuery(Guid RestaurantId) : IRequest<Result<List<CustomizationGroupSummaryDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record CustomizationGroupSummaryDto(
    Guid Id,
    string Name,
    int MinSelections,
    int MaxSelections,
    int ChoiceCount)
{
    public CustomizationGroupSummaryDto() : this(Guid.Empty, string.Empty, 0, 0, 0) { }
}
