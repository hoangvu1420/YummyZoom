using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemCustomizationAssigned(
    MenuItemId MenuItemId,
    CustomizationGroupId CustomizationGroupId,
    string DisplayTitle
) : DomainEventBase;
