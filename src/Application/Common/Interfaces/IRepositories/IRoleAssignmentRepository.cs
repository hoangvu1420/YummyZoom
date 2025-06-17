using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> GetByIdAsync(RoleAssignmentId id, CancellationToken cancellationToken = default);
    Task<List<RoleAssignment>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<RoleAssignment>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<RoleAssignment?> GetByUserRestaurantRoleAsync(UserId userId, RestaurantId restaurantId, RestaurantRole role, CancellationToken cancellationToken = default);
    Task AddAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default);
    Task DeleteAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default);
}
