using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.CouponAggregate.Events;

namespace YummyZoom.Application.Coupons.EventHandlers;

/// <summary>
/// Idempotent no-op handler for CouponUsed events emitted via outbox.
/// Serves as a processing sink and a convenient hook for future projections.
/// </summary>
public sealed class CouponUsedEventHandler : IdempotentNotificationHandler<CouponUsed>
{
    private readonly ILogger<CouponUsedEventHandler> _logger;

    public CouponUsedEventHandler(IUnitOfWork uow, IInboxStore inbox, ILogger<CouponUsedEventHandler> logger)
        : base(uow, inbox)
    {
        _logger = logger;
    }

    protected override Task HandleCore(CouponUsed notification, CancellationToken ct)
    {
        _logger.LogDebug("Processed CouponUsed via outbox: CouponId={CouponId}, Prev={Prev}, New={New}",
            notification.CouponId.Value, notification.PreviousUsageCount, notification.NewUsageCount);
        return Task.CompletedTask;
    }
}
