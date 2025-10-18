using YummyZoom.Application.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.MenuItems.Queries.Feed;

public sealed record GetMenuItemsFeedQuery(
    string Tab,
    int PageNumber,
    int PageSize
) : IRequest<Result<PaginatedList<MenuItemFeedDto>>>;

public sealed record MenuItemFeedDto(
    Guid ItemId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    string? ImageUrl,
    double? Rating,
    string RestaurantName,
    Guid RestaurantId,
    long LifetimeSoldCount
);

public static class GetMenuItemsFeedErrors
{
    public static Error InvalidTab => Error.Validation(
        "MenuItems.Feed.InvalidTab",
        "Tab must be one of: popular.");
}
