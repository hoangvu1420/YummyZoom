
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.MenuAggregate.Events;

public record MenuEnabled(MenuId MenuId) : IDomainEvent;
