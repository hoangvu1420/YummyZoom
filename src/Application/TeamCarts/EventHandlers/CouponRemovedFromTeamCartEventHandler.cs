using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles CouponRemovedFromTeamCart by clearing coupon info in the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class CouponRemovedFromTeamCartEventHandler : IdempotentNotificationHandler<CouponRemovedFromTeamCart>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<CouponRemovedFromTeamCartEventHandler> _logger;

    public CouponRemovedFromTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<CouponRemovedFromTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(CouponRemovedFromTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogInformation("Handling CouponRemovedFromTeamCart (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        try
        {
            await _store.RemoveCouponAsync(cartId, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove coupon in VM or notify (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }

        _logger.LogInformation("Handled CouponRemovedFromTeamCart (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);
    }
}

