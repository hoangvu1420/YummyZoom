using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IReviewRepository
{
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
    Task<Review?> GetByIdAsync(ReviewId reviewId, CancellationToken cancellationToken = default);
    Task<Review?> GetByCustomerAndRestaurantAsync(Guid customerId, Guid restaurantId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Review review, CancellationToken cancellationToken = default);
}

