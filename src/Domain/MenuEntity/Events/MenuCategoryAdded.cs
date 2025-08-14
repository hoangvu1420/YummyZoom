using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuCategoryAdded(MenuId MenuId, MenuCategoryId CategoryId) : DomainEventBase;
