namespace YummyZoom.Application.Reviews.Queries.Common;

public sealed record ReviewDto(
    Guid ReviewId,
    Guid AuthorUserId,
    int Rating,
    string? Title,
    string? Comment,
    DateTime SubmittedAtUtc
);

public sealed record RestaurantReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    int Ratings1,
    int Ratings2,
    int Ratings3,
    int Ratings4,
    int Ratings5,
    int TotalWithText,
    DateTime? LastReviewAtUtc,
    DateTime UpdatedAtUtc
);

