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
    IReadOnlyList<Guid>? TagIds = null) : IRequest<Result<PaginatedList<RestaurantSearchResultDto>>>;
