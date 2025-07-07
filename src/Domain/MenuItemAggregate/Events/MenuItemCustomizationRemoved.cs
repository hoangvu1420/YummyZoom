using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemCustomizationRemoved(
    MenuItemId MenuItemId,
    CustomizationGroupId CustomizationGroupId
) : IDomainEvent;
