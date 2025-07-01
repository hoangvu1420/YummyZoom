using YummyZoom.Domain.TagAggregate.ValueObjects;

namespace YummyZoom.Domain.TagAggregate.Events;

public record TagUpdated(TagId TagId, string TagName, string TagCategory) : IDomainEvent; 
