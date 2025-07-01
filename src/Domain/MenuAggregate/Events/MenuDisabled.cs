
using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuDisabled(MenuId MenuId) : IDomainEvent;
