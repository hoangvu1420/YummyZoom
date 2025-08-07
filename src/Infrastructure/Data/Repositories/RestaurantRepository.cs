using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RestaurantRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants
            .FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken);
    }
}
