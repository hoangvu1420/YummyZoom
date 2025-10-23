using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record AddItemToTeamCartCommand(
    Guid TeamCartId,
    Guid MenuItemId,
    int Quantity,
    IReadOnlyList<AddItemToTeamCartCustomizationSelection>? SelectedCustomizations = null,
    string? IdempotencyKey = null
) : IRequest<Result>, ITeamCartCommand, IIdempotentCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
};

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
