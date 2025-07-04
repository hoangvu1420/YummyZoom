using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationChoiceRemoved(
    CustomizationGroupId CustomizationGroupId,
    ChoiceId ChoiceId,
    string Name
) : IDomainEvent;
