using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewCreated(
    ReviewId ReviewId,
    OrderId OrderId,
    UserId CustomerId,
    RestaurantId RestaurantId,
    Rating Rating,
    DateTime SubmissionTimestamp) : IDomainEvent;
