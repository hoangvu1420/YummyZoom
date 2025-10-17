using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartCreated domain events by creating the initial TeamCart VM in the real-time store
/// and notifying connected clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartCreatedEventHandler : IdempotentNotificationHandler<TeamCartCreated>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartCreatedEventHandler> _logger;

    public TeamCartCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartRepository teamCartRepository,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartCreatedEventHandler> logger) : base(uow, inbox)
    {
        _teamCartRepository = teamCartRepository;
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartCreated notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartCreated (EventId={EventId}, CartId={CartId})", notification.EventId, cartId.Value);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("TeamCartCreated handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        var vm = new TeamCartViewModel
        {
            CartId = cart.Id,
            RestaurantId = cart.RestaurantId.Value,
            Status = cart.Status,
            Deadline = cart.Deadline,
            ExpiresAt = cart.ExpiresAt,
            ShareTokenMasked = cart.ShareToken.Value.Length >= 4 ? $"***{cart.ShareToken.Value[^4..]}" : "***",
            TipAmount = cart.TipAmount.Amount,
            TipCurrency = cart.TipAmount.Currency,
            CouponCode = null,
            DiscountAmount = 0m,
            DiscountCurrency = cart.TipAmount.Currency,
            Subtotal = 0m,
            Currency = cart.TipAmount.Currency,
            DeliveryFee = 0m,
            TaxAmount = 0m,
            Total = 0m,
            CashOnDeliveryPortion = 0m,
            Version = 1,
            Members = new List<TeamCartViewModel.Member>(),
            Items = new List<TeamCartViewModel.Item>()
        };

        // Best-effort: include host member if available in aggregate without heavy includes
        foreach (var m in cart.Members)
        {
            vm.Members.Add(new TeamCartViewModel.Member
            {
                UserId = m.UserId.Value,
                Name = m.Name,
                Role = m.Role.ToString(),
                PaymentStatus = "Pending",
                CommittedAmount = 0m,
                OnlineTransactionId = null
            });
        }

        try
        {
            await _store.CreateVmAsync(vm, ct);
            await _notifier.NotifyCartUpdated(cart.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create VM or notify for TeamCartId={CartId} (EventId={EventId})", cart.Id.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

