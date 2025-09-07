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
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<MemberJoinedEventHandler> _logger;

    public MemberJoinedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<MemberJoinedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(MemberJoined notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogInformation("Handling MemberJoined (EventId={EventId}, CartId={CartId}, UserId={UserId})", notification.EventId, cartId.Value, notification.UserId.Value);

        var member = new TeamCartViewModel.Member
        {
            UserId = notification.UserId.Value,
            Name = notification.Name,
            Role = "Guest",
            PaymentStatus = "Pending",
            CommittedAmount = 0m,
            OnlineTransactionId = null
        };

        try
        {
            await _store.AddMemberAsync(cartId, member, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add member to VM or notify (CartId={CartId}, UserId={UserId}, EventId={EventId})", cartId.Value, notification.UserId.Value, notification.EventId);
            throw; // allow outbox retry
        }

        _logger.LogInformation("Handled MemberJoined (EventId={EventId}, CartId={CartId}, UserId={UserId})", notification.EventId, cartId.Value, notification.UserId.Value);
    }
}

