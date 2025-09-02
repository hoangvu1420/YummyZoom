using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.Search.EventHandlers;

public sealed class MenuItemDeletedSearchHandler : IdempotentNotificationHandler<MenuItemDeleted>
{
    private readonly ISearchReadModelMaintainer _maintainer;

    public MenuItemDeletedSearchHandler(IUnitOfWork uow, IInboxStore inbox, ISearchReadModelMaintainer maintainer)
        : base(uow, inbox)
    {
        _maintainer = maintainer;
    }

    protected override async Task HandleCore(MenuItemDeleted e, CancellationToken ct)
    {
        await _maintainer.SoftDeleteByIdAsync(e.MenuItemId.Value, e.OccurredOnUtc.Ticks, ct);
    }
}
