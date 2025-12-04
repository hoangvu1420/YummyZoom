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
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TipAppliedToTeamCartEventHandler> _logger;

    public TipAppliedToTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TipAppliedToTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TipAppliedToTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        var tip = notification.TipAmount;

        _logger.LogDebug("Handling TipAppliedToTeamCart (EventId={EventId}, CartId={CartId}, Tip={Tip} {Currency})",
            notification.EventId, cartId.Value, tip.Amount, tip.Currency);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("TipAppliedToTeamCart handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            await _store.ApplyTipAsync(cartId, tip.Amount, tip.Currency, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                var push = await _pushNotifier.PushTeamCartDataAsync(cartId, vm.Version, ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply tip in VM or notify (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

