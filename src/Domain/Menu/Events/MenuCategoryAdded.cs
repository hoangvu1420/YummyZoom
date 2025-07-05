
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Menu.Events;

public record MenuCategoryAdded(MenuId MenuId, MenuCategoryId CategoryId) : IDomainEvent;
