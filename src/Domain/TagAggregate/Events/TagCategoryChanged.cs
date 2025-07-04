using YummyZoom.Domain.TagAggregate.ValueObjects;

namespace YummyZoom.Domain.TagAggregate.Events;

public record TagCategoryChanged(TagId TagId, string OldCategory, string NewCategory) : IDomainEvent;
