using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

/// <summary>
/// Repository abstraction for the RestaurantAccount aggregate.
/// </summary>
public interface IRestaurantAccountRepository
{
    Task<RestaurantAccount?> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<RestaurantAccount> GetOrCreateAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<RestaurantAccount> GetOrCreateAsync(RestaurantId restaurantId, string currency, CancellationToken cancellationToken = default);
    Task AddAsync(RestaurantAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(RestaurantAccount account, CancellationToken cancellationToken = default);
}
