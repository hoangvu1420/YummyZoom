using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class MemberCommittedToPaymentEventHandler : IdempotentNotificationHandler<MemberCommittedToPayment>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<MemberCommittedToPaymentEventHandler> _logger;

    public MemberCommittedToPaymentEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<MemberCommittedToPaymentEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(MemberCommittedToPayment notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var userId = notification.UserId;
        var amount = notification.Amount;

        _logger.LogInformation("Handling MemberCommittedToPayment (EventId={EventId}, CartId={CartId}, UserId={UserId}, Amount={Amount} {Currency})",
            notification.EventId, cartId.Value, userId.Value, amount.Amount, amount.Currency);

        try
        {
            await _store.CommitCodAsync(cartId, userId.Value, amount.Amount, amount.Currency, ct);
            await _notifier.NotifyPaymentEvent(cartId, userId.Value, "CODCommitted", ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update VM for MemberCommittedToPayment (CartId={CartId}, UserId={UserId}, EventId={EventId})",
                cartId.Value, userId.Value, notification.EventId);
            throw;
        }
    }
}
