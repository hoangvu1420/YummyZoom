using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;

/// <summary>
/// Lists menus for a restaurant with basic counts; requires staff authorization.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetMenusForManagementQuery(Guid RestaurantId)
    : IRequest<Result<IReadOnlyList<MenuSummaryDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuSummaryDto(
    Guid MenuId,
    string Name,
    string Description,
    bool IsEnabled,
    DateTimeOffset LastModified,
    int CategoryCount,
    int ItemCount);
