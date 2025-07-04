using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuCategoryUpdated(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
