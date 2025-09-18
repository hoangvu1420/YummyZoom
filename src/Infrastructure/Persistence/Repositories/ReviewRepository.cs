using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _db;

    public ReviewRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _db.Reviews.AddAsync(review, cancellationToken);
    }

    public async Task<Review?> GetByIdAsync(ReviewId reviewId, CancellationToken cancellationToken = default)
    {
        return await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<Review?> GetByCustomerAndRestaurantAsync(Guid customerId, Guid restaurantId, CancellationToken cancellationToken = default)
    {
        // Global query filter already excludes soft-deleted rows; avoid redundant IsDeleted predicate to keep translation simple.
        var uid = UserId.Create(customerId);
        var rid = RestaurantId.Create(restaurantId);
        return await _db.Reviews
            .FirstOrDefaultAsync(r => r.CustomerId == uid && r.RestaurantId == rid, cancellationToken);
    }

    public Task UpdateAsync(Review review, CancellationToken cancellationToken = default)
    {
        _db.Reviews.Update(review);
        return Task.CompletedTask;
    }
}
