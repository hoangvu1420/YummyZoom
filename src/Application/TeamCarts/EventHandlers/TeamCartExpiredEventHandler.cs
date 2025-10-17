using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartExpired by deleting the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartExpiredEventHandler : IdempotentNotificationHandler<TeamCartExpired>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartExpiredEventHandler> _logger;

    public TeamCartExpiredEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartExpiredEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartExpired notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartExpired (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        try
        {
            await _store.DeleteVmAsync(cartId, ct);
            await _notifier.NotifyExpired(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete VM or notify expired (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}


