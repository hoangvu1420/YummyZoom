
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuCreated(MenuId MenuId, RestaurantId RestaurantId) : IDomainEvent;
