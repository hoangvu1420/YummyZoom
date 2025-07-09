using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.Events;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity;

public sealed class MenuCategory : Entity<MenuCategoryId>, IAuditableEntity, ISoftDeletableEntity
{
    public MenuId MenuId { get; private set; }
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

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

        var menuCategory = new MenuCategory(
            MenuCategoryId.CreateUnique(),
            menuId,
            name,
            displayOrder);

        menuCategory.AddDomainEvent(new MenuCategoryAdded(
            menuCategory.MenuId,
            menuCategory.Id));

        return Result.Success(menuCategory);
    }

    public Result UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidCategoryName(name));

        Name = name;

        AddDomainEvent(new MenuCategoryNameUpdated(MenuId, Id, name));

        return Result.Success();
    }

    public Result UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder <= 0)
            return Result.Failure(MenuErrors.InvalidDisplayOrder(displayOrder));

        DisplayOrder = displayOrder;

        AddDomainEvent(new MenuCategoryDisplayOrderUpdated(MenuId, Id, displayOrder));

        return Result.Success();
    }

    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new MenuCategoryRemoved(MenuId, Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private MenuCategory() { }
#pragma warning restore CS8618
}
