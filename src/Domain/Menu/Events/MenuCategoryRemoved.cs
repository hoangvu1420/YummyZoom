
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuCategoryRemoved(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
