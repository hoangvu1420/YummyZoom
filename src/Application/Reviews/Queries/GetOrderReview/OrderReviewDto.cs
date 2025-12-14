namespace YummyZoom.Application.Reviews.Queries.GetOrderReview;

public record OrderReviewDto(
    Guid ReviewId,
    Guid OrderId,
    Guid RestaurantId,
    int Rating,
    string? Title,
    string? Comment,
    DateTime CreatedAt);
