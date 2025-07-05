using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuEnabled(MenuId MenuId) : IDomainEvent;
