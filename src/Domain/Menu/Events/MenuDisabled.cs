
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuDisabled(MenuId MenuId) : IDomainEvent;
