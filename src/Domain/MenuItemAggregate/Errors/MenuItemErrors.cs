using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Errors;

public static class MenuItemErrors
{
    public static readonly Error NegativePrice = Error.Validation(
        "MenuItem.NegativePrice", "Menu item price must be positive.");

    public static Error InvalidName(string? name) => Error.Validation(
        "MenuItem.InvalidName",
        $"Item name '{name ?? "null"}' is invalid. Item name cannot be null or empty.");

    public static Error InvalidDescription(string? description) => Error.Validation(
        "MenuItem.InvalidDescription",
        $"Item description '{description ?? "null"}' is invalid. Item description cannot be null or empty.");

    public static Error DuplicateItemName(string itemName, string categoryName) => Error.Conflict(
        "MenuItem.DuplicateItemName",
        $"An item with the name '{itemName}' already exists in category '{categoryName}'.");

    public static Error NotFound(string itemId, string categoryName) => Error.NotFound(
        "MenuItem.NotFound",
        $"The item with ID '{itemId}' was not found in category '{categoryName}'.");

    public static Error CustomizationAlreadyAssigned(string groupId) => Error.Conflict(
        "MenuItem.CustomizationAlreadyAssigned",
        $"Customization group '{groupId}' is already assigned to this menu item.");

    public static Error CustomizationNotFound(string groupId) => Error.NotFound(
        "MenuItem.CustomizationNotFound",
        $"Customization group '{groupId}' is not assigned to this menu item.");

    public static Error CategoryNotFound(Guid categoryId) => Error.NotFound(
        "MenuItem.CategoryNotFound",
        $"Menu category with ID '{categoryId}' was not found.");

    public static Error CategoryNotBelongsToRestaurant(Guid categoryId, Guid restaurantId) => Error.Validation(
        "MenuItem.CategoryNotBelongsToRestaurant",
        $"Menu category '{categoryId}' does not belong to restaurant '{restaurantId}'.");
}
