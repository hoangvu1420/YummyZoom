using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IPayoutRepository
{
    Task AddAsync(Payout payout, CancellationToken cancellationToken = default);
    Task<Payout?> GetByIdAsync(PayoutId payoutId, CancellationToken cancellationToken = default);
    Task<Payout?> GetByIdempotencyKeyAsync(RestaurantId restaurantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLatestCompletedAtAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLatestRequestedAtAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default);
}
