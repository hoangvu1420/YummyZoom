using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemDetailsUpdated(
    MenuItemId MenuItemId,
    string Name,
    string Description,
    string? ImageUrl
) : DomainEventBase;
