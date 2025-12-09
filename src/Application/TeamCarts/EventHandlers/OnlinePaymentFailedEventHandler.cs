using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class OnlinePaymentFailedEventHandler : IdempotentNotificationHandler<OnlinePaymentFailed>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<OnlinePaymentFailedEventHandler> _logger;

    public OnlinePaymentFailedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<OnlinePaymentFailedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OnlinePaymentFailed notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var userId = notification.UserId;
        var amount = notification.Amount;

        _logger.LogDebug("Handling OnlinePaymentFailed (EventId={EventId}, CartId={CartId}, UserId={UserId}, Amount={Amount} {Currency})",
            notification.EventId, cartId.Value, userId.Value, amount.Amount, amount.Currency);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("OnlinePaymentFailed handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            // Update RT VM to reflect failure and notify clients
            await _store.RecordOnlinePaymentFailureAsync(cartId, userId.Value, ct);
            await _notifier.NotifyPaymentEvent(cartId, userId.Value, "OnlineFailed", ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                var context = new TeamCartNotificationContext
                {
                    EventType = "OnlinePaymentFailed",
                    ActorUserId = userId.Value,
                    Amount = notification.Amount.Amount,
                    Currency = notification.Amount.Currency
                };
                
                // Notify the payer only (hybrid)
                var push = await _pushNotifier.PushTeamCartDataAsync(
                    cartId, 
                    vm.Version, 
                    TeamCartNotificationTarget.SpecificUser,
                    context,
                    NotificationDeliveryType.Hybrid,
                    ct);
                
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OnlinePaymentFailed (CartId={CartId}, UserId={UserId}, EventId={EventId})",
                cartId.Value, userId.Value, notification.EventId);
            throw;
        }
    }
}
