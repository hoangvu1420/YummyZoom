using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.TagEntity.Events;

public record TagUpdated(TagId TagId, string TagName, string TagCategory) : IDomainEvent; 
