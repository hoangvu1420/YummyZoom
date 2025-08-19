using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface ICustomizationGroupRepository
{
    Task<IReadOnlyList<CustomizationGroup>> GetByIdsAsync(IEnumerable<CustomizationGroupId> ids, CancellationToken cancellationToken = default);
}
