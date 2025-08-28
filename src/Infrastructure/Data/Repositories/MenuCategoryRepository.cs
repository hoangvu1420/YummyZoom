using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class MenuCategoryRepository : IMenuCategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MenuCategoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public async Task<List<MenuCategory>> GetByMenuIdAsync(MenuId menuId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuCategories
            .Where(c => c.MenuId == menuId && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MenuCategory>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuCategories
            .Where(c => _dbContext.Menus
                .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted)
                .Select(m => m.Id)
                .Contains(c.MenuId) && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MenuCategory category, CancellationToken cancellationToken = default)
    {
        await _dbContext.MenuCategories.AddAsync(category, cancellationToken);
    }

    public void Update(MenuCategory category)
    {
        _dbContext.MenuCategories.Update(category);
    }
}
