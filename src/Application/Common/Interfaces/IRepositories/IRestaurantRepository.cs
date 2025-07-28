using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
} 
