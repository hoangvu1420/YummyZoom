using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Extensions;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class CustomizationGroupRepository : ICustomizationGroupRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CustomizationGroupRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<CustomizationGroup>> GetByIdsAsync(IEnumerable<CustomizationGroupId> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<CustomizationGroup>();
        }

        // Include owned choices so that selection validation and snapshotting can occur without lazy loading.
        var groups = await _dbContext.CustomizationGroups
            .Where(g => idList.Contains(g.Id))
            .Include(g => g.Choices)
            .ToListAsync(cancellationToken);
        return groups;
    }

    public async Task<RestaurantId?> GetRestaurantIdByIdIncludingDeletedAsync(CustomizationGroupId id, CancellationToken cancellationToken = default)
    {
        var restaurantGuid = await _dbContext.CustomizationGroups
            .IncludeSoftDeleted()
            .Where(g => g.Id == id)
            .Select(g => g.RestaurantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return restaurantGuid == default ? null : RestaurantId.Create(restaurantGuid);
    }

    public async Task<CustomizationGroup?> GetByIdAsync(CustomizationGroupId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomizationGroups
            .Include(g => g.Choices)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task AddAsync(CustomizationGroup group, CancellationToken cancellationToken = default)
    {
        await _dbContext.CustomizationGroups.AddAsync(group, cancellationToken);
    }

    public void Update(CustomizationGroup group)
    {
        _dbContext.CustomizationGroups.Update(group);
    }
}
