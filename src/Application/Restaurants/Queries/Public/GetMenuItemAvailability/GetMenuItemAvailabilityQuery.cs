using YummyZoom.Application.Common.Caching;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;

public sealed record GetMenuItemAvailabilityQuery(Guid RestaurantId, Guid ItemId)
    : IRequest<Result<MenuItemAvailabilityDto>>, ICacheableQuery<Result<MenuItemAvailabilityDto>>
{
    public string CacheKey => $"restaurant:{RestaurantId:N}:menu-item-availability:v1:{ItemId:N}";
    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromSeconds(15), $"restaurant:{RestaurantId:N}:availability");
}

public sealed record MenuItemAvailabilityDto(
    Guid RestaurantId,
    Guid ItemId,
    bool IsAvailable,
    int? Stock,
    DateTimeOffset CheckedAt,
    int TtlSeconds);

