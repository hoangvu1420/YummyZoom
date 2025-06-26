
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Errors;

public static class Menu
{
    public static readonly Error InvalidMenuId = Error.Validation(
        "Menu.InvalidMenuId", "The menu id is invalid.");
}
