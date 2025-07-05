using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.Menu.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu;

public sealed class MenuCategory : Entity<MenuCategoryId>
{
    public MenuId MenuId { get; private set; }
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }

    private MenuCategory(
        MenuCategoryId categoryId,
        MenuId menuId,
        string name,
        int displayOrder)
        : base(categoryId)
    {
        MenuId = menuId;
        Name = name;
        DisplayOrder = displayOrder;
    }

    public static Result<MenuCategory> Create(
        MenuId menuId,
        string name,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuCategory>(MenuErrors.InvalidCategoryName(name));

        if (displayOrder <= 0)
            return Result.Failure<MenuCategory>(MenuErrors.InvalidDisplayOrder(displayOrder));

        return Result.Success(new MenuCategory(
            MenuCategoryId.CreateUnique(),
            menuId,
            name,
            displayOrder));
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidCategoryName(name));

        Name = name;
        return Result.Success();
    }

    public Result UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder <= 0)
            return Result.Failure(MenuErrors.InvalidDisplayOrder(displayOrder));

        DisplayOrder = displayOrder;
        return Result.Success();
    }

#pragma warning disable CS8618
    private MenuCategory() { }
#pragma warning restore CS8618
}
