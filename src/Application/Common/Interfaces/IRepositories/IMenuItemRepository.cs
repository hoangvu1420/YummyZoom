using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default);
    Task<List<MenuItem>> GetByIdsAsync(List<MenuItemId> menuItemIds, CancellationToken cancellationToken = default);
    Task<List<MenuItem>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default);
} 
