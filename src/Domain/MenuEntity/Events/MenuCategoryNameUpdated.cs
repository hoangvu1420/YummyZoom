
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuCategoryNameUpdated(MenuId MenuId, MenuCategoryId CategoryId, string NewName) : DomainEventBase;
