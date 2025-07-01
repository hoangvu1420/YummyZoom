using YummyZoom.Domain.TagAggregate.ValueObjects;

namespace YummyZoom.Domain.TagAggregate.Events;

public record TagCreated(TagId TagId, string TagName, string TagCategory) : IDomainEvent; 
