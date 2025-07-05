using YummyZoom.Domain.ReviewAggregate.ValueObjects;

namespace YummyZoom.Domain.ReviewAggregate.Events;

public record ReviewDeleted(ReviewId ReviewId) : IDomainEvent;
