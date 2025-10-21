using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.SearchRestaurants;

public sealed record SearchRestaurantsQuery(
    string? Q,
    string? Cuisine,
    double? Lat,
    double? Lng,
    double? RadiusKm,
    int PageNumber,
    int PageSize,
    double? MinRating = null,
    string? Sort = null,
    string? Bbox = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<Guid>? TagIds = null,
    bool? DiscountedOnly = null,
    bool IncludeFacets = false) : IRequest<Result<SearchRestaurantsResult>>;

public abstract record SearchRestaurantsResult;

public sealed record RestaurantSearchPageResult(PaginatedList<RestaurantSearchResultDto> Page) : SearchRestaurantsResult;

public sealed record RestaurantSearchWithFacetsDto(
    PaginatedList<RestaurantSearchResultDto> Page,
    RestaurantFacetsDto Facets) : SearchRestaurantsResult;

public sealed record RestaurantFacetsDto(
    IReadOnlyList<FacetCount<string>> Cuisines,
    IReadOnlyList<FacetCount<string>> Tags,
    IReadOnlyList<FacetCount<short>> PriceBands,
    int OpenNowCount);

public sealed record FacetCount<T>(T Value, int Count);
