using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class OnlinePaymentSucceededEventHandler : IdempotentNotificationHandler<OnlinePaymentSucceeded>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<OnlinePaymentSucceededEventHandler> _logger;

    public OnlinePaymentSucceededEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<OnlinePaymentSucceededEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OnlinePaymentSucceeded notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var userId = notification.UserId;
        var amount = notification.Amount;
        var transactionId = notification.TransactionId;

        _logger.LogInformation("Handling OnlinePaymentSucceeded (EventId={EventId}, CartId={CartId}, UserId={UserId}, Amount={Amount} {Currency}, Tx={Tx})",
            notification.EventId, cartId.Value, userId.Value, amount.Amount, amount.Currency, transactionId);

        try
        {
            await _store.RecordOnlinePaymentAsync(cartId, userId.Value, amount.Amount, amount.Currency, transactionId, ct);
            await _notifier.NotifyPaymentEvent(cartId, userId.Value, "OnlineSucceeded", ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update VM for OnlinePaymentSucceeded (CartId={CartId}, UserId={UserId}, EventId={EventId})",
                cartId.Value, userId.Value, notification.EventId);
            throw;
        }
    }
}
