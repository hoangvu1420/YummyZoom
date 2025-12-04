using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles MemberJoined domain events by adding the member to the TeamCart VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class MemberJoinedEventHandler : IdempotentNotificationHandler<MemberJoined>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<MemberJoinedEventHandler> _logger;

    public MemberJoinedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<MemberJoinedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(MemberJoined notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling MemberJoined (EventId={EventId}, CartId={CartId}, UserId={UserId})", notification.EventId, cartId.Value, notification.UserId.Value);

        var member = new TeamCartViewModel.Member
        {
            UserId = notification.UserId.Value,
            Name = notification.Name,
            Role = "Guest",
            PaymentStatus = "Pending",
            CommittedAmount = 0m,
            OnlineTransactionId = null
        };

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("MemberJoined handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            await _store.AddMemberAsync(cartId, member, ct);
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
            _logger.LogError(ex, "Failed to add member to VM or notify (CartId={CartId}, UserId={UserId}, EventId={EventId})", cartId.Value, notification.UserId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

