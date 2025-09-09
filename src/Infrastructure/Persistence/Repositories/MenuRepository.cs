using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MenuRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Menu?> GetByIdAsync(MenuId menuId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .FirstOrDefaultAsync(m => m.Id == menuId, cancellationToken);
    }

    public async Task<Menu?> GetEnabledByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .Where(m => m.RestaurantId == restaurantId && m.IsEnabled && !m.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Menu>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        await _dbContext.Menus.AddAsync(menu, cancellationToken);
    }

    public void Update(Menu menu)
    {
        _dbContext.Menus.Update(menu);
    }
}
