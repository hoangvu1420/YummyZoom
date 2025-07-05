using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemPriceChanged(
    MenuItemId MenuItemId,
    Money NewPrice
) : IDomainEvent;
