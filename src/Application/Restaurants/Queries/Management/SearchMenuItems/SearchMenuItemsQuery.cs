using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;

/// <summary>
/// Searches menu items for a restaurant across categories with optional filters.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record SearchMenuItemsQuery(
    Guid RestaurantId,
    Guid? MenuCategoryId,
    string? Q,
    bool? IsAvailable,
    int PageNumber,
    int PageSize) : IRequest<Result<PaginatedList<MenuItemSearchResultDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuItemSearchResultDto(
    Guid ItemId,
    Guid MenuCategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsAvailable,
    string? ImageUrl,
    DateTimeOffset LastModified);

public static class SearchMenuItemsErrors
{
    public static Error CategoryNotFound => Error.NotFound(
        "Management.SearchMenuItems.CategoryNotFound",
        "Menu category was not found for the restaurant.");
}
