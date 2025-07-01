using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CustomizationGroupAggregate.Events;

public record CustomizationGroupCreated(
    CustomizationGroupId CustomizationGroupId,
    RestaurantId RestaurantId,
    string GroupName
) : IDomainEvent; 
