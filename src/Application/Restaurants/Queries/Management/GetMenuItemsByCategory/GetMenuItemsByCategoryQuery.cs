using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;

/// <summary>
/// Lists menu items within a category for a restaurant; requires staff authorization.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetMenuItemsByCategoryQuery(
    Guid RestaurantId,
    Guid MenuCategoryId,
    string? Q,
    bool? IsAvailable,
    int PageNumber,
    int PageSize) : IRequest<Result<PaginatedList<MenuItemSummaryDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuItemSummaryDto(
    Guid ItemId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsAvailable,
    string? ImageUrl,
    DateTimeOffset LastModified);

public static class GetMenuItemsByCategoryErrors
{
    public static Error NotFound => Error.NotFound(
        "Management.GetMenuItemsByCategory.NotFound",
        "Menu category was not found for the restaurant.");
}
