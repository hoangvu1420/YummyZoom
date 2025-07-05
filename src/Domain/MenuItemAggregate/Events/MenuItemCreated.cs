using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemCreated(
    MenuItemId MenuItemId,
    RestaurantId RestaurantId,
    MenuCategoryId MenuCategoryId
) : IDomainEvent;
