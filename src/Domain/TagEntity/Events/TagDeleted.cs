using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.TagEntity.Events;

public record TagDeleted(TagId TagId) : IDomainEvent;
