using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuCategoryRemoved(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
