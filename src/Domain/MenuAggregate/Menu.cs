using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Events;
using YummyZoom.Domain.MenuAggregate.Errors;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;

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

    public static Result<Menu> Create(
        RestaurantId restaurantId,
        string name,
        string description,
        bool isEnabled = true,
        List<MenuCategory>? categories = null)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Menu>(MenuErrors.InvalidMenuName(name));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<Menu>(MenuErrors.InvalidMenuDescription(description));

        // Validate categories if provided
        var validatedCategories = categories ?? [];
        
        // Check for duplicate category names (case-insensitive)
        var duplicateCategory = validatedCategories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        
        if (duplicateCategory != null)
            return Result.Failure<Menu>(MenuErrors.DuplicateCategoryName(duplicateCategory.Key));

        var menu = new Menu(
            MenuId.CreateUnique(),
            restaurantId,
            name,
            description,
            isEnabled,
            validatedCategories);
        
        menu.AddDomainEvent(new MenuCreated((MenuId)menu.Id, menu.RestaurantId));
        
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
        AddDomainEvent(new MenuEnabled((MenuId)Id));
    }

    public void Disable()
    {
        if (!IsEnabled) return; 
        IsEnabled = false;
        AddDomainEvent(new MenuDisabled((MenuId)Id));
    }

    // Category Management Methods
    public Result<MenuCategory> AddCategory(string name, string description, int displayOrder)
    {
        // Create the new category
        var categoryResult = MenuCategory.Create(name, displayOrder);
        if (categoryResult.IsFailure)
            return Result.Failure<MenuCategory>(categoryResult.Error);

        var category = categoryResult.Value;

        // Check for duplicate category names (case-insensitive)
        if (_categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<MenuCategory>(MenuErrors.DuplicateCategoryName(name));

        _categories.Add(category);
        AddDomainEvent(new MenuCategoryAdded((MenuId)Id, category.Id));
        
        return Result.Success(category);
    }

    public Result RemoveCategory(MenuCategoryId categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        // Business rule: Cannot remove category that has items
        if (category.HasItems)
            return Result.Failure(MenuErrors.CannotRemoveCategoryWithItems(categoryId.Value.ToString()));

        _categories.Remove(category);
        AddDomainEvent(new MenuCategoryRemoved((MenuId)Id, categoryId));
        
        return Result.Success();
    }

    public Result<MenuCategory> GetCategory(MenuCategoryId categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure<MenuCategory>(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        return Result.Success(category);
    }

    public Result UpdateCategoryDetails(MenuCategoryId categoryId, string name, int displayOrder)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        // Check for duplicate category names (case-insensitive), excluding current category
        if (_categories.Any(c => c.Id != categoryId && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(MenuErrors.DuplicateCategoryName(name));

        var updateResult = category.UpdateCategoryDetails(name, displayOrder);
        if (updateResult.IsFailure)
            return updateResult;

        AddDomainEvent(new MenuCategoryUpdated((MenuId)Id, categoryId));
        
        return Result.Success();
    }

    // Item Management Methods (through categories)
    public Result<MenuItem> AddItemToCategory(MenuCategoryId categoryId, string name, string description, Money basePrice, 
        string? imageUrl = null, bool isAvailable = true)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure<MenuItem>(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        // Create the menu item
        var itemResult = MenuItem.Create(name, description, basePrice, imageUrl, isAvailable);
        if (itemResult.IsFailure)
            return Result.Failure<MenuItem>(itemResult.Error);

        var item = itemResult.Value;

        // Add item to category (this will validate uniqueness within category)
        var addResult = category.AddMenuItem(item);
        if (addResult.IsFailure)
            return Result.Failure<MenuItem>(addResult.Error);

        AddDomainEvent(new MenuItemAdded((MenuId)Id, categoryId, item.Id));
        
        return Result.Success(item);
    }

    public Result RemoveItemFromCategory(MenuCategoryId categoryId, MenuItemId itemId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        var removeResult = category.RemoveMenuItem(itemId);
        if (removeResult.IsFailure)
            return removeResult;

        AddDomainEvent(new MenuItemRemoved((MenuId)Id, categoryId, itemId));
        
        return Result.Success();
    }

    public Result<MenuItem> GetMenuItem(MenuCategoryId categoryId, MenuItemId itemId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure<MenuItem>(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        return category.GetMenuItem(itemId);
    }

    public Result<MenuItem> GetMenuItemFromAnyCategory(MenuItemId itemId)
    {
        foreach (var category in _categories)
        {
            var itemResult = category.GetMenuItem(itemId);
            if (itemResult.IsSuccess)
                return itemResult;
        }

        return Result.Failure<MenuItem>(MenuErrors.ItemNotFound(itemId.Value.ToString(), "any category"));
    }

    public Result UpdateMenuItem(MenuCategoryId categoryId, MenuItemId itemId, string name, string description, Money basePrice, string? imageUrl = null)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        var itemResult = category.GetMenuItem(itemId);
        if (itemResult.IsFailure)
            return Result.Failure(itemResult.Error);

        var item = itemResult.Value;

        // Check for duplicate item names within the category (excluding current item)
        if (category.Items.Any(i => i.Id != itemId && i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(MenuErrors.DuplicateItemName(name, category.Name));

        var updateResult = item.UpdateFullDetails(name, description, basePrice, imageUrl);
        if (updateResult.IsFailure)
            return updateResult;

        AddDomainEvent(new MenuItemUpdated((MenuId)Id, categoryId, itemId));
        
        return Result.Success();
    }

    public Result UpdateMenuItemPrice(MenuCategoryId categoryId, MenuItemId itemId, Money newPrice)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        var itemResult = category.GetMenuItem(itemId);
        if (itemResult.IsFailure)
            return Result.Failure(itemResult.Error);

        var item = itemResult.Value;

        var updateResult = item.UpdatePrice(newPrice);
        if (updateResult.IsFailure)
            return updateResult;

        AddDomainEvent(new MenuItemUpdated((MenuId)Id, categoryId, itemId));
        
        return Result.Success();
    }

    public Result ToggleMenuItemAvailability(MenuCategoryId categoryId, MenuItemId itemId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return Result.Failure(MenuErrors.CategoryNotFound(categoryId.Value.ToString()));

        var itemResult = category.GetMenuItem(itemId);
        if (itemResult.IsFailure)
            return Result.Failure(itemResult.Error);

        var item = itemResult.Value;

        if (item.IsAvailable)
            item.MarkAsUnavailable();
        else
            item.MarkAsAvailable();

        AddDomainEvent(new MenuItemUpdated((MenuId)Id, categoryId, itemId));
        
        return Result.Success();
    }

    // Query Methods
    public bool HasCategories => _categories.Count > 0;
    public int CategoryCount => _categories.Count;
    public int TotalItemCount => _categories.Sum(c => c.ItemCount);

    public IReadOnlyList<MenuCategory> GetCategoriesOrderedByDisplayOrder()
    {
        return _categories.OrderBy(c => c.DisplayOrder).ToList().AsReadOnly();
    }

    public IReadOnlyList<MenuItem> GetAllItems()
    {
        return _categories.SelectMany(c => c.Items).ToList().AsReadOnly();
    }

    public IReadOnlyList<MenuItem> GetAvailableItems()
    {
        return _categories.SelectMany(c => c.Items)
            .Where(item => item.IsAvailable)
            .ToList().AsReadOnly();
    }

    public IReadOnlyList<MenuItem> GetUnavailableItems()
    {
        return _categories.SelectMany(c => c.Items)
            .Where(item => !item.IsAvailable)
            .ToList().AsReadOnly();
    }

    public IReadOnlyList<MenuItem> GetItemsByCategory(MenuCategoryId categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        return category?.Items ?? new List<MenuItem>().AsReadOnly();
    }

    public IReadOnlyList<MenuItem> SearchItemsByName(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<MenuItem>().AsReadOnly();

        return _categories.SelectMany(c => c.Items)
            .Where(item => item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList().AsReadOnly();
    }

    public bool IsMenuEmpty => !HasCategories || TotalItemCount == 0;

    public int AvailableItemCount => _categories.Sum(c => c.Items.Count(i => i.IsAvailable));

    public int UnavailableItemCount => _categories.Sum(c => c.Items.Count(i => !i.IsAvailable));

#pragma warning disable CS8618
    private Menu() { }
#pragma warning restore CS8618
}
