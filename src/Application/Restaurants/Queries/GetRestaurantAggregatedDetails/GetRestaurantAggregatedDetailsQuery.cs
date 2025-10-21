using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantAggregatedDetails;

public sealed record GetRestaurantAggregatedDetailsQuery(
    Guid RestaurantId,
    double? Lat = null,
    double? Lng = null)
    : IRequest<Result<RestaurantAggregatedDetailsDto>>, ICacheableQuery<Result<RestaurantAggregatedDetailsDto>>
{
    public string CacheKey => (Lat.HasValue || Lng.HasValue)
        ? string.Empty
        : $"restaurant:details:v1:{RestaurantId:N}";

    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromMinutes(2), $"restaurant:{RestaurantId:N}:details");
}

public static class GetRestaurantAggregatedDetailsErrors
{
    public static Error NotFound(Guid restaurantId) =>
        Error.NotFound("Public.GetRestaurantAggregatedDetails.NotFound", $"Restaurant {restaurantId} was not found.");
}
