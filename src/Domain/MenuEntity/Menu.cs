using YummyZoom.Domain.MenuEntity.Errors;
using YummyZoom.Domain.MenuEntity.Events;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity;

public sealed class Menu : Entity<MenuId>, IAuditableEntity, ISoftDeletableEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public RestaurantId RestaurantId { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    private Menu(
        MenuId menuId,
        RestaurantId restaurantId,
        string name,
        string description,
        bool isEnabled)
        : base(menuId)
    {
        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
    }

    public static Result<Menu> Create(
        RestaurantId restaurantId,
        string name,
        string description,
        bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Menu>(MenuErrors.InvalidMenuName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<Menu>(MenuErrors.InvalidMenuDescription(description));

        var menu = new Menu(
            MenuId.CreateUnique(),
            restaurantId,
            name,
            description,
            isEnabled);
        
        menu.AddDomainEvent(new MenuCreated(menu.Id, menu.RestaurantId));
        
        return Result.Success(menu);
    }
    
    public Result UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidMenuName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(MenuErrors.InvalidMenuDescription(description));

        Name = name;
        Description = description;
        return Result.Success();
    }

    public void Enable()
    {
        if (IsEnabled) return; 
        IsEnabled = true;
        AddDomainEvent(new MenuEnabled(Id));
    }

    public void Disable()
    {
        if (!IsEnabled) return; 
        IsEnabled = false;
        AddDomainEvent(new MenuDisabled(Id));
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

        AddDomainEvent(new MenuRemoved(Id, RestaurantId));

        return Result.Success();
    }

#pragma warning disable CS8618
    private Menu() { }
#pragma warning restore CS8618
}
