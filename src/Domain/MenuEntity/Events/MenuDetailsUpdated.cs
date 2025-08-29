using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuDetailsUpdated(
    MenuId MenuId,
    RestaurantId RestaurantId,
    string Name,
    string Description) : DomainEventBase;
