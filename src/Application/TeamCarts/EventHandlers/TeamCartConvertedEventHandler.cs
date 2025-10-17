using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class TeamCartConvertedEventHandler : IdempotentNotificationHandler<TeamCartConverted>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartConvertedEventHandler> _logger;

    public TeamCartConvertedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartConvertedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartConverted notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartConverted (EventId={EventId}, CartId={CartId}, OrderId={OrderId})",
            notification.EventId, cartId.Value, notification.OrderId.Value);

        try
        {
            await _store.DeleteVmAsync(cartId, ct);
            await _notifier.NotifyConverted(cartId, notification.OrderId.Value, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TeamCartConverted (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw;
        }
    }
}


