using YummyZoom.Domain.TagAggregate.ValueObjects;

namespace YummyZoom.Domain.TagAggregate.Events;

public record TagDeleted(TagId TagId) : IDomainEvent;
