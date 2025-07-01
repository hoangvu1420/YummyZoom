
using YummyZoom.Domain.MenuAggregate.ValueObjects;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuEnabled(MenuId MenuId) : IDomainEvent;
