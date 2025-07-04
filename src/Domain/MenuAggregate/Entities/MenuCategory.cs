using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Errors;
using YummyZoom.SharedKernel;

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

    public static Result<MenuCategory> Create(
        string name,
        int displayOrder,
        List<MenuItem>? items = null)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<MenuCategory>(MenuErrors.InvalidCategoryName(name));

        if (displayOrder <= 0)
            return Result.Failure<MenuCategory>(MenuErrors.InvalidDisplayOrder(displayOrder));

        // Validate items if provided
        var validatedItems = items ?? [];
        
        // Check for duplicate item names within this category (case-insensitive)
        var duplicateItem = validatedItems
            .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        
        if (duplicateItem != null)
            return Result.Failure<MenuCategory>(MenuErrors.DuplicateItemName(duplicateItem.Key, name));

        return Result.Success(new MenuCategory(
            MenuCategoryId.CreateUnique(), 
            name, 
            displayOrder, 
            validatedItems));
    }
    
    public Result AddMenuItem(MenuItem menuItem)
    {
        if (_items.Any(i => i.Name.Equals(menuItem.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(MenuErrors.DuplicateItemName(menuItem.Name, Name));
        
        _items.Add(menuItem);
        return Result.Success();
    }

    public Result RemoveMenuItem(MenuItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure(MenuErrors.ItemNotFound(itemId.Value.ToString(), Name));
        
        _items.Remove(item);
        return Result.Success();
    }

    public Result<MenuItem> GetMenuItem(MenuItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure<MenuItem>(MenuErrors.ItemNotFound(itemId.Value.ToString(), Name));
        
        return Result.Success(item);
    }

    public Result UpdateCategoryDetails(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(MenuErrors.InvalidCategoryName(name));

        if (displayOrder <= 0)
            return Result.Failure(MenuErrors.InvalidDisplayOrder(displayOrder));

        Name = name;
        DisplayOrder = displayOrder;
        return Result.Success();
    }

    public bool HasItems => _items.Count > 0;

    public int ItemCount => _items.Count;
    
#pragma warning disable CS8618
    private MenuCategory() { }
#pragma warning restore CS8618
}
