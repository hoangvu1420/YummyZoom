
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuDisabled(MenuId MenuId) : IDomainEvent;
