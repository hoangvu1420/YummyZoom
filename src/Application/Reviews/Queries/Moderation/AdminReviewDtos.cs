namespace YummyZoom.Application.Reviews.Queries.Moderation;

using YummyZoom.Application.Common.Models;

public sealed record AdminModerationReviewDto(
    Guid ReviewId,
    Guid RestaurantId,
    string RestaurantName,
    Guid CustomerId,
    int Rating,
    string? Comment,
    DateTime SubmissionTimestamp,
    bool IsModerated,
    bool IsHidden,
    DateTime? LastActionAtUtc
);

public sealed record AdminModerationReviewDetailDto(
    Guid ReviewId,
    Guid RestaurantId,
    string RestaurantName,
    Guid CustomerId,
    int Rating,
    string? Comment,
    string? Reply,
    Guid? OrderId,
    double? RestaurantAverageRating,
    int? RestaurantTotalReviews,
    DateTime SubmissionTimestamp,
    bool IsModerated,
    bool IsHidden,
    DateTime? LastActionAtUtc
);

public sealed record ReviewModerationAuditDto(
    string Action,
    string? Reason,
    Guid ActorUserId,
    string? ActorDisplayName,
    DateTime TimestampUtc
);


