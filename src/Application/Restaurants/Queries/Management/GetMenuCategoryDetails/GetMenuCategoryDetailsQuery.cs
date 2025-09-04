using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;

/// <summary>
/// Retrieves a menu category details for a restaurant; requires staff authorization.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetMenuCategoryDetailsQuery(Guid RestaurantId, Guid MenuCategoryId)
    : IRequest<Result<MenuCategoryDetailsDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuCategoryDetailsDto(
    Guid MenuId,
    string MenuName,
    Guid CategoryId,
    string Name,
    int DisplayOrder,
    int ItemCount,
    DateTimeOffset LastModified);

public static class GetMenuCategoryDetailsErrors
{
    public static Error NotFound => Error.NotFound(
        "Management.GetMenuCategoryDetails.NotFound",
        "Menu category was not found for the restaurant.");
}
