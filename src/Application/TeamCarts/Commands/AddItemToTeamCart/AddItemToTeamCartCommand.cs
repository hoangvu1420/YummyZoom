using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;

[Authorize]
public sealed record AddItemToTeamCartCommand(
    Guid TeamCartId,
    Guid MenuItemId,
    int Quantity,
    IReadOnlyList<AddItemToTeamCartCustomizationSelection>? SelectedCustomizations = null
) : IRequest<Result<Unit>>
;

public sealed record AddItemToTeamCartCustomizationSelection(Guid GroupId, Guid ChoiceId);

public static class AddItemToTeamCartErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "AddItemToTeamCart.MenuItemNotFound",
        $"Menu item '{menuItemId}' was not found.");

    public static Error MenuItemUnavailable(Guid menuItemId) => Error.Validation(
        "AddItemToTeamCart.MenuItemUnavailable",
        $"Menu item '{menuItemId}' is not available.");

    public static Error MenuItemNotBelongsToRestaurant(Guid menuItemId, Guid restaurantId) => Error.Validation(
        "AddItemToTeamCart.MenuItemWrongRestaurant",
        $"Menu item '{menuItemId}' does not belong to restaurant '{restaurantId}'.");

    public static Error CustomizationGroupNotFound(Guid groupId) => Error.NotFound(
        "AddItemToTeamCart.CustomizationGroupNotFound",
        $"Customization group '{groupId}' was not found.");

    public static Error CustomizationChoiceNotFound(Guid groupId, Guid choiceId) => Error.NotFound(
        "AddItemToTeamCart.CustomizationChoiceNotFound",
        $"Customization choice '{choiceId}' was not found in group '{groupId}'.");

    public static Error CustomizationGroupNotAppliedToMenuItem(Guid groupId, Guid menuItemId) => Error.Validation(
        "AddItemToTeamCart.CustomizationGroupNotApplied",
        $"Customization group '{groupId}' is not applied to menu item '{menuItemId}'.");
}
