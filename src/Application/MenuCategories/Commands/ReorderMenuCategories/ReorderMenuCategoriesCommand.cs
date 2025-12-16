using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuCategories.Commands.ReorderMenuCategories;

/// <summary>
/// Bulk-updates display order for menu categories in a restaurant after validating all provided categories belong to the restaurant.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record ReorderMenuCategoriesCommand(
    Guid RestaurantId,
    IReadOnlyList<CategoryOrderDto> CategoryOrders
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record CategoryOrderDto(Guid CategoryId, int DisplayOrder);

public static class ReorderMenuCategoriesErrors
{
    public static Error CategoryNotFound(Guid categoryId) => Error.NotFound(
        "Menu.Reorder.CategoryNotFound",
        $"The category with ID '{categoryId}' was not found for the restaurant.");

    public static Error IncompleteCategoryList => Error.Validation(
        "Menu.Reorder.IncompleteCategoryList",
        "The request must include all categories for the restaurant.");

    public static Error InvalidDisplayOrderRange(int expectedCount) => Error.Validation(
        "Menu.Reorder.InvalidDisplayOrderRange",
        $"DisplayOrder values must be a unique, gapless sequence from 1 to {expectedCount}.");
}