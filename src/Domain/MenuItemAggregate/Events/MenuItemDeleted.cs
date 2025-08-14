using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemDeleted(MenuItemId MenuItemId) : DomainEventBase;
