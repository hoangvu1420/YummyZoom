using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

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
}
