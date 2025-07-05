using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemAssignedToCategory(
    MenuItemId MenuItemId,
    MenuCategoryId OldCategoryId,
    MenuCategoryId NewCategoryId
) : IDomainEvent;
