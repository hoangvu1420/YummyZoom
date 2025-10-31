using YummyZoom.Application.Common.Caching;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;

public sealed record GetMenuItemPublicDetailsQuery(Guid RestaurantId, Guid ItemId)
    : IRequest<Result<MenuItemPublicDetailsDto>>, ICacheableQuery<Result<MenuItemPublicDetailsDto>>
{
    public string CacheKey => $"restaurant:{RestaurantId:N}:menu-item:v1:{ItemId:N}";
    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromMinutes(2), $"restaurant:{RestaurantId:N}:menu", $"restaurant:{RestaurantId:N}:items");
}

public sealed record MenuItemPublicDetailsDto(
    Guid RestaurantId,
    Guid ItemId,
    string Name,
    string Description,
    string? ImageUrl,
    decimal BasePrice,
    string Currency,
    bool IsAvailable,
    long SoldCount,
    double? Rating,
    int? ReviewCount,
    IReadOnlyList<CustomizationGroupDto> CustomizationGroups,
    string? NotesHint,
    ItemQuantityLimits Limits,
    IReadOnlyList<UpsellSuggestionDto> Upsell,
    DateTimeOffset LastModified
);

public sealed record CustomizationGroupDto(
    Guid GroupId,
    string Name,
    string Type,
    bool Required,
    int Min,
    int Max,
    IReadOnlyList<CustomizationChoiceDto> Items
);

public sealed record CustomizationChoiceDto(
    Guid Id,
    string Name,
    decimal PriceDelta,
    bool Default,
    bool OutOfStock
);

public sealed record ItemQuantityLimits(int MinQty, int MaxQty);

public sealed record UpsellSuggestionDto(Guid ItemId, string Name, decimal Price, string? ImageUrl);

