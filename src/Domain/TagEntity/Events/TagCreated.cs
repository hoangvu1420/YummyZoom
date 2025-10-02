using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.TagEntity.Events;

public record TagCreated(TagId TagId, string TagName, string TagCategory) : DomainEventBase;
