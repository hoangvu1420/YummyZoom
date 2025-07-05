using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuCreated(MenuId MenuId, RestaurantId RestaurantId) : IDomainEvent;
