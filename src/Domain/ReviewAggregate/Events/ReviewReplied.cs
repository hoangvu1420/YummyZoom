using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewReplied(
    ReviewId ReviewId,
    string Reply,
    DateTime RepliedAt) : DomainEventBase;
