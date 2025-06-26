using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Events;

namespace YummyZoom.Domain.MenuAggregate;

public sealed class Menu : AggregateRoot<MenuId, Guid>
{
    private readonly List<MenuCategory> _categories = [];

    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    
    public IReadOnlyList<MenuCategory> Categories => _categories.AsReadOnly();

    private Menu(
        MenuId menuId,
        RestaurantId restaurantId,
        string name,
        string description,
        bool isEnabled,
        List<MenuCategory> categories)
        : base(menuId)
    {
        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
        _categories = categories;
    }

    public static Menu Create(
        RestaurantId restaurantId,
        string name,
        string description,
        bool isEnabled = true,
        List<MenuCategory>? categories = null)
    {
        var menu = new Menu(
            MenuId.CreateUnique(),
            restaurantId,
            name,
            description,
            isEnabled,
            categories ?? []);
        
        menu.AddDomainEvent(new MenuCreated((MenuId)menu.Id, menu.RestaurantId));
        
        return menu;
    }
    
    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void Enable()
    {
        if (IsEnabled) return; 
        IsEnabled = true;
        AddDomainEvent(new MenuEnabled((MenuId)Id));
    }

    public void Disable()
    {
        if (!IsEnabled) return; 
        IsEnabled = false;
        AddDomainEvent(new MenuDisabled((MenuId)Id));
    }

#pragma warning disable CS8618
    private Menu() { }
#pragma warning restore CS8618
}
