using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuItemAggregate.Events;

public record MenuItemAvailabilityChanged(
    MenuItemId MenuItemId,
    bool IsAvailable
) : IDomainEvent;
