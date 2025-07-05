using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.TagEntity.Events;

public record TagCategoryChanged(TagId TagId, string OldCategory, string NewCategory) : IDomainEvent;
