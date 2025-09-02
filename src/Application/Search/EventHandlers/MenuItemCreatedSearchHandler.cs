using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.Search.EventHandlers;

public sealed class MenuItemCreatedSearchHandler : IdempotentNotificationHandler<MenuItemCreated>
{
    private readonly ISearchReadModelMaintainer _maintainer;

    public MenuItemCreatedSearchHandler(IUnitOfWork uow, IInboxStore inbox, ISearchReadModelMaintainer maintainer)
        : base(uow, inbox)
    {
        _maintainer = maintainer;
    }

    protected override async Task HandleCore(MenuItemCreated e, CancellationToken ct)
    {
        await _maintainer.UpsertMenuItemByIdAsync(e.MenuItemId.Value, e.OccurredOnUtc.Ticks, ct);
    }
}
