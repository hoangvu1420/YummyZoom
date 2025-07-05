
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuCreated(MenuId MenuId, RestaurantId RestaurantId) : IDomainEvent;
