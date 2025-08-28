using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IMenuCategoryRepository
{
    /// <summary>
    /// Gets a menu category by its ID.
    /// </summary>
    Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all categories for a specific menu.
    /// </summary>
    Task<List<MenuCategory>> GetByMenuIdAsync(MenuId menuId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all categories for a specific restaurant (across all menus).
    /// </summary>
    Task<List<MenuCategory>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new menu category to the repository.
    /// </summary>
    Task AddAsync(MenuCategory category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing menu category. Uses EF Core change tracking.
    /// </summary>
    void Update(MenuCategory category);
}
