using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public sealed class TeamCartRepository : ITeamCartRepository
{
    private readonly ApplicationDbContext _db;

    public TeamCartRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Domain.TeamCartAggregate.TeamCart?> GetByIdAsync(TeamCartId id, CancellationToken cancellationToken = default)
    {
        return _db.TeamCarts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Domain.TeamCartAggregate.TeamCart cart, CancellationToken cancellationToken = default)
    {
        await _db.TeamCarts.AddAsync(cart, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TeamCart cart, CancellationToken cancellationToken = default)
    {
        _db.TeamCarts.Update(cart);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamCart>> GetExpiringCartsAsync(DateTime cutoffUtc, int take, CancellationToken cancellationToken = default)
    {
        return await _db.TeamCarts
            .Where(c => (c.Status == TeamCartStatus.Open || c.Status == TeamCartStatus.Locked)
                        && c.ExpiresAt <= cutoffUtc)
            .OrderBy(c => c.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

