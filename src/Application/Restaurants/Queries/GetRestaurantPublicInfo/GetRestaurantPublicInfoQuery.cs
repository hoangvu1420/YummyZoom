using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed record GetRestaurantPublicInfoQuery(Guid RestaurantId) : IRequest<Result<RestaurantPublicInfoDto>>;

public static class GetRestaurantPublicInfoErrors
{
    public static Error NotFound => Error.NotFound(
        "Public.GetRestaurantPublicInfo.NotFound", "Restaurant info was not found.");
}
