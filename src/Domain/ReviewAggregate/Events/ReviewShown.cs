using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewShown(
    ReviewId ReviewId,
    DateTime ShownAt) : IDomainEvent;
