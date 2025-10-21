using YummyZoom.Application.Reviews.Queries.Common;

namespace YummyZoom.Application.Restaurants.Queries.Common;

public sealed record RestaurantPublicInfoDto(
    Guid RestaurantId,
    string Name,
    string? LogoUrl,
    string? BackgroundImageUrl,
    string Description,
    string CuisineType,
    IReadOnlyList<string> CuisineTags,
    bool IsAcceptingOrders,
    bool IsVerified,
    AddressDto Address,
    ContactInfoDto ContactInfo,
    string BusinessHours,
    DateTimeOffset EstablishedDate,
    DateTimeOffset LastModified,
    decimal? DistanceKm = null);

public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);

public sealed record ContactInfoDto(
    string PhoneNumber,
    string Email);

public sealed record RestaurantAggregatedDetailsDto(
    RestaurantPublicInfoDto Info,
    RestaurantAggregatedMenuDto Menu,
    RestaurantReviewSummaryDto ReviewSummary,
    DateTimeOffset LastChangedUtc);

public sealed record RestaurantAggregatedMenuDto(
    string MenuJson,
    DateTimeOffset LastRebuiltAt);

public sealed record RestaurantSearchResultDto(
    Guid RestaurantId,
    string Name,
    string? LogoUrl,
    IReadOnlyList<string> CuisineTags,
    decimal? AvgRating,
    int? RatingCount,
    string? City,
    decimal? DistanceKm = null,
    double? Latitude = null,
    double? Longitude = null);
