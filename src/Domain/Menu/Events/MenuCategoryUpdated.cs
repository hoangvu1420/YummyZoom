
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuCategoryUpdated(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
