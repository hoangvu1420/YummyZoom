using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationChoiceAdded(
    CustomizationGroupId CustomizationGroupId,
    ChoiceId ChoiceId,
    string Name
) : IDomainEvent; 
