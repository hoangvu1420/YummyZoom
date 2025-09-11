using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class OnlinePaymentFailedEventHandler : IdempotentNotificationHandler<OnlinePaymentFailed>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<OnlinePaymentFailedEventHandler> _logger;

    public OnlinePaymentFailedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<OnlinePaymentFailedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OnlinePaymentFailed notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var userId = notification.UserId;
        var amount = notification.Amount;

        _logger.LogInformation("Handling OnlinePaymentFailed (EventId={EventId}, CartId={CartId}, UserId={UserId}, Amount={Amount} {Currency})",
            notification.EventId, cartId.Value, userId.Value, amount.Amount, amount.Currency);

        try
        {
            // Update RT VM to reflect failure and notify clients
            await _store.RecordOnlinePaymentFailureAsync(cartId, userId.Value, ct);
            await _notifier.NotifyPaymentEvent(cartId, userId.Value, "OnlineFailed", ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OnlinePaymentFailed (CartId={CartId}, UserId={UserId}, EventId={EventId})",
                cartId.Value, userId.Value, notification.EventId);
            throw;
        }
    }
}
