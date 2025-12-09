using Dapper;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public sealed class TeamCartRepository : ITeamCartRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public TeamCartRepository(ApplicationDbContext db, IDbConnectionFactory dbConnectionFactory)
    {
        _db = db;
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
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

    public async Task<IReadOnlyList<TeamCartMembershipInfo>> GetActiveTeamCartMembershipsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                tc."Id" AS "TeamCartId",
                tcm."Role" AS "Role"
            FROM "TeamCartMembers" tcm
            INNER JOIN "TeamCarts" tc ON tcm."TeamCartId" = tc."Id"
            WHERE tcm."UserId" = @UserId
              AND tc."Status" IN ('Open', 'Locked')
              AND tc."ExpiresAt" > @NowUtc
            ORDER BY tc."CreatedAt" DESC
            """;

        var memberships = await connection.QueryAsync<TeamCartMembershipInfo>(
            new CommandDefinition(sql,
                new { UserId = userId, NowUtc = DateTime.UtcNow },
                cancellationToken: cancellationToken));

        return memberships.ToList();
    }
}
