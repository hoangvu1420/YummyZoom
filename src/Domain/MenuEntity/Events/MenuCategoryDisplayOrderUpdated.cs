using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuEntity.Events;

public record MenuCategoryDisplayOrderUpdated(MenuId MenuId, MenuCategoryId CategoryId, int NewDisplayOrder) : DomainEventBase;
