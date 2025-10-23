using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles ItemRemovedFromTeamCart events to broadcast updated coupon suggestions.
/// </summary>
public sealed class TeamCartCouponSuggestionsOnItemRemovedEventHandler : IdempotentNotificationHandler<ItemRemovedFromTeamCart>
{
    private readonly TeamCartCouponSuggestionsService _couponSuggestionsService;
    private readonly TeamCartCouponSuggestionsOptions _options;
    private readonly ILogger<TeamCartCouponSuggestionsOnItemRemovedEventHandler> _logger;

    public TeamCartCouponSuggestionsOnItemRemovedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        TeamCartCouponSuggestionsService couponSuggestionsService,
        IOptions<TeamCartCouponSuggestionsOptions> options,
        ILogger<TeamCartCouponSuggestionsOnItemRemovedEventHandler> logger) : base(uow, inbox)
    {
        _couponSuggestionsService = couponSuggestionsService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemRemovedFromTeamCart notification, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _couponSuggestionsService.BroadcastCouponSuggestionsAsync(
            notification.TeamCartId, 
            notification.RemovedByUserId, 
            "ItemRemoved", 
            ct);
    }
}
