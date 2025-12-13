using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

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

    public async Task<Review?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var oid = OrderId.Create(orderId);
        return await _db.Reviews.FirstOrDefaultAsync(r => r.OrderId == oid, cancellationToken);
    }

    public Task UpdateAsync(Review review, CancellationToken cancellationToken = default)
    {
        _db.Reviews.Update(review);
        return Task.CompletedTask;
    }
}
