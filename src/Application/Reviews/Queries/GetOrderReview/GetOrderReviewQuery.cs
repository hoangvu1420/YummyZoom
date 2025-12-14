using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Reviews.Queries.GetOrderReview;

public record GetOrderReviewQuery(Guid OrderId) : IRequest<Result<OrderReviewDto>>;
