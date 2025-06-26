
using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Entities;

public sealed class MenuCategory : Entity<MenuCategoryId>
{
    private readonly List<MenuItem> _items = [];
    
    public string Name { get; private set; }
    public int DisplayOrder { get; private set; }
    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();

    private MenuCategory(
        MenuCategoryId categoryId,
        string name, 
        int displayOrder, 
        List<MenuItem> items)
        : base(categoryId)
    {
        Name = name;
        DisplayOrder = displayOrder;
        _items = items;
    }

    public static MenuCategory Create(
        string name,
        int displayOrder,
        List<MenuItem>? items = null)
    {
        return new MenuCategory(
            MenuCategoryId.CreateUnique(), 
            name, 
            displayOrder, 
            items ?? []);
    }
    
#pragma warning disable CS8618
    private MenuCategory() { }
#pragma warning restore CS8618
}
