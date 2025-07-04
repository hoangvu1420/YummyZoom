using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuItemRemoved(MenuId MenuId, MenuCategoryId CategoryId, MenuItemId ItemId) : IDomainEvent;
