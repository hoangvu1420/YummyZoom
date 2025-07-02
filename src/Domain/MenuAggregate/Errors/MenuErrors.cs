
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Errors;

public static class MenuErrors
{
    public static readonly Error InvalidMenuId = Error.Validation(
        "Menu.InvalidMenuId", "The menu id is invalid.");

    public static readonly Error NegativeMenuItemPrice = Error.Validation(
        "Menu.NegativeMenuItemPrice", "Menu item price must be positive.");
}
