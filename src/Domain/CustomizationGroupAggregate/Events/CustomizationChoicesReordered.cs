using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationChoicesReordered(
    CustomizationGroupId CustomizationGroupId,
    Dictionary<ChoiceId, int> ReorderedChoices
) : DomainEventBase;
