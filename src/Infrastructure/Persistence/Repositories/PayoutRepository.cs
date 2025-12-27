using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class PayoutRepository : IPayoutRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PayoutRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payouts.AddAsync(payout, cancellationToken);
    }

    public async Task<Payout?> GetByIdAsync(PayoutId payoutId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts.FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken);
    }

    public async Task<Payout?> GetByIdempotencyKeyAsync(RestaurantId restaurantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts.FirstOrDefaultAsync(
            p => p.RestaurantId == restaurantId && p.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLatestCompletedAtAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .Where(p => p.RestaurantId == restaurantId && p.CompletedAt != null)
            .OrderByDescending(p => p.CompletedAt)
            .Select(p => p.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLatestRequestedAtAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payouts
            .Where(p => p.RestaurantId == restaurantId)
            .OrderByDescending(p => p.RequestedAt)
            .Select(p => p.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(payout);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Payouts.Attach(payout);
        }
        return Task.CompletedTask;
    }
}
