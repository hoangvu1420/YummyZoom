using YummyZoom.Application.Common.Caching;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetFullMenu;

public sealed record GetFullMenuQuery(Guid RestaurantId)
    : IRequest<Result<GetFullMenuResponse>>, ICacheableQuery<Result<GetFullMenuResponse>>
{
    public string CacheKey => $"restaurant:menu:v1:{RestaurantId:N}";
    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromMinutes(5), $"restaurant:{RestaurantId:N}:menu");
}

public sealed record GetFullMenuResponse(string MenuJson, DateTimeOffset LastRebuiltAt);

public static class GetFullMenuErrors
{
    public static Error NotFound => Error.NotFound(
        "Public.GetFullMenu.NotFound", "Menu view for the restaurant was not found.");
}
