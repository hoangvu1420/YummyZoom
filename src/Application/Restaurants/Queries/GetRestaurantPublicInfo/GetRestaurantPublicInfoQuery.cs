using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed record GetRestaurantPublicInfoQuery(
    Guid RestaurantId,
    double? Lat = null,
    double? Lng = null)
    : IRequest<Result<RestaurantPublicInfoDto>>, ICacheableQuery<Result<RestaurantPublicInfoDto>>
{
    // When lat/lng are provided, we bypass caching to get personalized distance
    // Only cache the basic info without distance calculation
    public string CacheKey => (Lat.HasValue || Lng.HasValue)
        ? string.Empty // Empty cache key bypasses caching
        : $"restaurant:public-info:v1:{RestaurantId:N}";
    
    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromMinutes(2), $"restaurant:{RestaurantId:N}:public-info");
}

public static class GetRestaurantPublicInfoErrors
{
    public static Error NotFound => Error.NotFound(
        "Public.GetRestaurantPublicInfo.NotFound", "Restaurant info was not found.");
}
