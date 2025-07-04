using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuCategoryAdded(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
