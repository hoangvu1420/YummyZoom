using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartLockedForPayment by setting the VM status to Locked and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartLockedForPaymentEventHandler : IdempotentNotificationHandler<TeamCartLockedForPayment>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartLockedForPaymentEventHandler> _logger;

    public TeamCartLockedForPaymentEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartLockedForPaymentEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartLockedForPayment notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartLockedForPayment (EventId={EventId}, CartId={CartId}, HostUserId={HostUserId})",
            notification.EventId, cartId.Value, notification.HostUserId.Value);

        try
        {
            await _store.SetLockedAsync(cartId, ct);
            await _notifier.NotifyLocked(cartId, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set locked in VM or notify (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

