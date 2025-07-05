using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuCategoryRemoved(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
