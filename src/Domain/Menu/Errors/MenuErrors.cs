
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Errors;

public static class MenuErrors
{
    public static readonly Error InvalidMenuId = Error.Validation(
        "Menu.InvalidMenuId", "The menu id is invalid.");

    public static Error InvalidMenuName(string? name) => Error.Validation(
        "Menu.InvalidMenuName", 
        $"Menu name '{name ?? "null"}' is invalid. Menu name cannot be null or empty.");

    public static Error InvalidMenuDescription(string? description) => Error.Validation(
        "Menu.InvalidMenuDescription", 
        $"Menu description '{description ?? "null"}' is invalid. Menu description cannot be null or empty.");

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

}
