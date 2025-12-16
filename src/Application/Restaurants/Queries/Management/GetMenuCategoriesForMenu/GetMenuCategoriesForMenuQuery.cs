using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoriesForMenu;

/// <summary>
/// Lists categories within a menu for a restaurant; requires staff authorization.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetMenuCategoriesForMenuQuery(Guid RestaurantId, Guid MenuId)
    : IRequest<Result<IReadOnlyList<MenuCategorySummaryDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuCategorySummaryDto(
    Guid CategoryId,
    string Name,
    int DisplayOrder,
    int ItemCount);

public static class GetMenuCategoriesForMenuErrors
{
    public static Error MenuNotFound => Error.NotFound(
        "Menu.InvalidMenuId",
        "Menu was not found for the restaurant.");
}
