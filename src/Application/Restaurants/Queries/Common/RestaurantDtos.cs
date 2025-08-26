namespace YummyZoom.Application.Restaurants.Queries.Common;

public sealed record RestaurantPublicInfoDto(
    Guid RestaurantId,
    string Name,
    string? LogoUrl,
    IReadOnlyList<string> CuisineTags,
    bool IsAcceptingOrders,
    string? City);

public sealed record RestaurantSearchResultDto(
    Guid RestaurantId,
    string Name,
    string? LogoUrl,
    IReadOnlyList<string> CuisineTags,
    decimal? AvgRating,
    int? RatingCount,
    string? City);
