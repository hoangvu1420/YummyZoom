using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IMenuRepository
{
    /// <summary>
    /// Gets a menu by its ID.
    /// </summary>
    Task<Menu?> GetByIdAsync(MenuId menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the enabled menu for a specific restaurant.
    /// </summary>
    Task<Menu?> GetEnabledByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all menus for a specific restaurant.
    /// </summary>
    Task<List<Menu>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new menu to the repository.
    /// </summary>
    Task AddAsync(Menu menu, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing menu. Uses EF Core change tracking.
    /// </summary>
    void Update(Menu menu);
}
