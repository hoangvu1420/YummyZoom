using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuDisabled(MenuId MenuId) : IDomainEvent;
