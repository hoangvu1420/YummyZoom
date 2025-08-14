using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewHidden(
    ReviewId ReviewId,
    DateTime HiddenAt) : DomainEventBase;
