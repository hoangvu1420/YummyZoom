using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewModerated(
    ReviewId ReviewId,
    DateTime ModeratedAt) : DomainEventBase;
