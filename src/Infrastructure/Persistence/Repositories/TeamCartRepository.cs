using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public sealed class TeamCartRepository : ITeamCartRepository
{
    private readonly ApplicationDbContext _db;

    public TeamCartRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<TeamCart?> GetByIdAsync(TeamCartId id, CancellationToken cancellationToken = default)
    {
        return _db.TeamCarts
            .Include(c => c.Members)
            .Include(c => c.Items)
            .Include(c => c.MemberPayments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(TeamCart cart, CancellationToken cancellationToken = default)
    {
        await _db.TeamCarts.AddAsync(cart, cancellationToken);
    }

    public async Task UpdateAsync(TeamCart cart, CancellationToken cancellationToken = default)
    {
        _db.TeamCarts.Update(cart);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TeamCart>> GetExpiringCartsAsync(DateTime cutoffUtc, int take, CancellationToken cancellationToken = default)
    {
        return await _db.TeamCarts
            .Where(c => (c.Status == TeamCartStatus.Open || c.Status == TeamCartStatus.Locked)
                        && c.ExpiresAt <= cutoffUtc)
            .OrderBy(c => c.ExpiresAt)
            .Take(take)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }
}
