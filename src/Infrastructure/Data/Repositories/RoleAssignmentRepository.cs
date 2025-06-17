using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class RoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly ApplicationDbContext _context;

    public RoleAssignmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoleAssignment?> GetByIdAsync(RoleAssignmentId id, CancellationToken cancellationToken = default)
    {
        return await _context.RoleAssignments
            .FirstOrDefaultAsync(ra => ra.Id == id, cancellationToken);
    }

    public async Task<List<RoleAssignment>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.RoleAssignments
            .Where(ra => ra.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoleAssignment>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _context.RoleAssignments
            .Where(ra => ra.RestaurantId == restaurantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleAssignment?> GetByUserRestaurantRoleAsync(UserId userId, RestaurantId restaurantId, RestaurantRole role, CancellationToken cancellationToken = default)
    {
        return await _context.RoleAssignments
            .FirstOrDefaultAsync(ra => ra.UserId == userId && ra.RestaurantId == restaurantId && ra.Role == role, cancellationToken);
    }

    public async Task AddAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default)
    {
        await _context.RoleAssignments.AddAsync(roleAssignment, cancellationToken);
    }

    public Task UpdateAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default)
    {
        _context.RoleAssignments.Update(roleAssignment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default)
    {
        _context.RoleAssignments.Remove(roleAssignment);
        return Task.CompletedTask;
    }
}
