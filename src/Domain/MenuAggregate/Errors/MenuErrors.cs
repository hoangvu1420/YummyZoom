using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Errors;

public static class MenuErrors
{
    // Existing errors (keeping static for backwards compatibility)
    public static readonly Error InvalidMenuId = Error.Validation(
        "Menu.InvalidMenuId", "The menu id is invalid.");

    public static readonly Error NegativeMenuItemPrice = Error.Validation(
        "Menu.NegativeMenuItemPrice", "Menu item price must be positive.");

    // Menu validation errors
    public static Error InvalidMenuName(string? name) => Error.Validation(
        "Menu.InvalidMenuName", 
        $"Menu name '{name ?? "null"}' is invalid. Menu name cannot be null or empty.");

    public static Error InvalidMenuDescription(string? description) => Error.Validation(
        "Menu.InvalidMenuDescription", 
        $"Menu description '{description ?? "null"}' is invalid. Menu description cannot be null or empty.");

    // Category errors
    public static Error InvalidCategoryName(string? name) => Error.Validation(
        "Menu.InvalidCategoryName", 
        $"Category name '{name ?? "null"}' is invalid. Category name cannot be null or empty.");

    public static Error DuplicateCategoryName(string name) => Error.Conflict(
        "Menu.DuplicateCategoryName", 
        $"A category with the name '{name}' already exists in this menu.");

    public static Error CategoryNotFound(string categoryId) => Error.NotFound(
        "Menu.CategoryNotFound", 
        $"The category with ID '{categoryId}' was not found in this menu.");

    public static Error InvalidDisplayOrder(int displayOrder) => Error.Validation(
        "Menu.InvalidDisplayOrder", 
        $"Display order '{displayOrder}' is invalid. Display order must be a positive number.");

    public static Error CategoryHasItems(string categoryName, int itemCount) => Error.Conflict(
        "Menu.CategoryHasItems", 
        $"Cannot remove category '{categoryName}' because it contains {itemCount} menu item(s).");

    // Item errors
    public static Error DuplicateItemName(string itemName, string categoryName) => Error.Conflict(
        "Menu.DuplicateItemName", 
        $"An item with the name '{itemName}' already exists in category '{categoryName}'.");

    public static Error ItemNotFound(string itemId, string categoryName) => Error.NotFound(
        "Menu.ItemNotFound", 
        $"The item with ID '{itemId}' was not found in category '{categoryName}'.");

    public static Error InvalidItemName(string? name) => Error.Validation(
        "Menu.InvalidItemName", 
        $"Item name '{name ?? "null"}' is invalid. Item name cannot be null or empty.");

    public static Error InvalidItemDescription(string? description) => Error.Validation(
        "Menu.InvalidItemDescription", 
        $"Item description '{description ?? "null"}' is invalid. Item description cannot be null or empty.");

    public static Error CannotRemoveCategoryWithItems(string categoryId) => Error.Conflict(
        "Menu.CannotRemoveCategoryWithItems", 
        $"Cannot remove category with ID '{categoryId}' because it contains menu items.");
}
