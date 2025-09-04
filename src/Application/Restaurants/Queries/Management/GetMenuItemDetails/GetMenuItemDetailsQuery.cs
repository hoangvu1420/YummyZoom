using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;

/// <summary>
/// Retrieves full details for a menu item; requires staff authorization.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetMenuItemDetailsQuery(Guid RestaurantId, Guid MenuItemId)
    : IRequest<Result<MenuItemDetailsDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuItemDetailsDto(
    Guid ItemId,
    Guid CategoryId,
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    bool IsAvailable,
    string? ImageUrl,
    IReadOnlyList<Guid> DietaryTagIds,
    IReadOnlyList<MenuItemCustomizationRefDto> AppliedCustomizations,
    DateTimeOffset LastModified);

public sealed record MenuItemCustomizationRefDto(Guid GroupId, string DisplayTitle, int DisplayOrder);

public static class GetMenuItemDetailsErrors
{
    public static Error NotFound => Error.NotFound(
        "Management.GetMenuItemDetails.NotFound",
        "Menu item was not found for the restaurant.");
}
