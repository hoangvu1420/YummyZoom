using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Restaurant restaurant, CancellationToken cancellationToken = default);
}
