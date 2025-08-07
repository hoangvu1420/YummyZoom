using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MenuItemRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<MenuItem?> GetByIdAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
    }

    public async Task<List<MenuItem>> GetByIdsAsync(List<MenuItemId> menuItemIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MenuItem>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAvailableAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default)
    {
        var menuItem = await _dbContext.MenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
        
        return menuItem is not null && menuItem.IsAvailable;
    }
} 
