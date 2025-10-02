using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed record GetRestaurantPublicInfoQuery(Guid RestaurantId)
    : IRequest<Result<RestaurantPublicInfoDto>>, ICacheableQuery<Result<RestaurantPublicInfoDto>>
{
    public string CacheKey => $"restaurant:public-info:v1:{RestaurantId:N}";
    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromMinutes(2), $"restaurant:{RestaurantId:N}:public-info");
}

public static class GetRestaurantPublicInfoErrors
{
    public static Error NotFound => Error.NotFound(
        "Public.GetRestaurantPublicInfo.NotFound", "Restaurant info was not found.");
}
