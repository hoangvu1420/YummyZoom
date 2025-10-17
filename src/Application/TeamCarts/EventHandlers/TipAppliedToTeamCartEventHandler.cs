using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TipAppliedToTeamCart events by updating the tip in the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TipAppliedToTeamCartEventHandler : IdempotentNotificationHandler<TipAppliedToTeamCart>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TipAppliedToTeamCartEventHandler> _logger;

    public TipAppliedToTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TipAppliedToTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TipAppliedToTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var tip = notification.TipAmount;

        _logger.LogDebug("Handling TipAppliedToTeamCart (EventId={EventId}, CartId={CartId}, Tip={Tip} {Currency})",
            notification.EventId, cartId.Value, tip.Amount, tip.Currency);

        try
        {
            await _store.ApplyTipAsync(cartId, tip.Amount, tip.Currency, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply tip in VM or notify (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

