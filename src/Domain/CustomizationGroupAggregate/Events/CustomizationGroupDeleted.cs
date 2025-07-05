using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationGroupDeleted(CustomizationGroupId CustomizationGroupId) : IDomainEvent;
