using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationChoiceAdded(
    CustomizationGroupId CustomizationGroupId,
    ChoiceId ChoiceId,
    string Name
) : DomainEventBase;
