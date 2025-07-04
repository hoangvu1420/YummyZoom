using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationChoiceUpdated(
    CustomizationGroupId CustomizationGroupId,
    ChoiceId ChoiceId,
    string NewName,
    Money NewPriceAdjustment,
    bool IsDefault
) : IDomainEvent;
