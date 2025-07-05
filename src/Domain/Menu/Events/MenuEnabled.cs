
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuEnabled(MenuId MenuId) : IDomainEvent;
