using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles CouponAppliedToTeamCart by updating the Redis VM with the coupon code and an optional discount estimate,
/// then notifying connected clients. Idempotent via inbox infrastructure.
/// </summary>
public sealed class CouponAppliedToTeamCartEventHandler : IdempotentNotificationHandler<CouponAppliedToTeamCart>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<CouponAppliedToTeamCartEventHandler> _logger;

    public CouponAppliedToTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartRepository teamCartRepository,
        ICouponRepository couponRepository,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<CouponAppliedToTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _teamCartRepository = teamCartRepository;
        _couponRepository = couponRepository;
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(CouponAppliedToTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogInformation("Handling CouponAppliedToTeamCart (EventId={EventId}, CartId={CartId}, CouponId={CouponId})",
            notification.EventId, cartId.Value, notification.CouponId.Value);

        // Load cart for currency
        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("CouponAppliedToTeamCart handler could not find cart (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            return;
        }

        // Load coupon to get its code; fallback to masked id if not found
        var coupon = await _couponRepository.GetByIdAsync(notification.CouponId, ct);
        var couponCode = coupon?.Code ?? $"#{notification.CouponId.Value.ToString()[..8]}";

        // For MVP, discountAmount is 0 and currency comes from cart context
        var discountAmount = 0m;
        var currency = cart.TipAmount.Currency; // consistent domain currency

        try
        {
            await _store.ApplyCouponAsync(cartId, couponCode, discountAmount, currency, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply coupon in VM or notify (CartId={CartId}, CouponCode={CouponCode}, EventId={EventId})",
                cartId.Value, couponCode, notification.EventId);
            throw; // allow outbox retry
        }

        _logger.LogInformation("Handled CouponAppliedToTeamCart (EventId={EventId}, CartId={CartId}, CouponCode={CouponCode})",
            notification.EventId, cartId.Value, couponCode);
    }
}

