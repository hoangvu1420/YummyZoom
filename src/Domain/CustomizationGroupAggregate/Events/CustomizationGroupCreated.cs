using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationGroupCreated(
    CustomizationGroupId CustomizationGroupId,
    RestaurantId RestaurantId,
    string GroupName
) : DomainEventBase; 
