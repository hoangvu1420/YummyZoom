namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IReviewSummaryMaintainer
{
    Task RecomputeForRestaurantAsync(Guid restaurantId, long sourceVersion, CancellationToken ct = default);
    Task RecomputeForReviewAsync(Guid reviewId, long sourceVersion, CancellationToken ct = default);
}

