using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles ItemQuantityUpdatedInTeamCart events to broadcast updated coupon suggestions.
/// </summary>
public sealed class TeamCartCouponSuggestionsOnItemQuantityUpdatedEventHandler : IdempotentNotificationHandler<ItemQuantityUpdatedInTeamCart>
{
    private readonly TeamCartCouponSuggestionsService _couponSuggestionsService;
    private readonly TeamCartCouponSuggestionsOptions _options;
    private readonly ILogger<TeamCartCouponSuggestionsOnItemQuantityUpdatedEventHandler> _logger;

    public TeamCartCouponSuggestionsOnItemQuantityUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        TeamCartCouponSuggestionsService couponSuggestionsService,
        IOptions<TeamCartCouponSuggestionsOptions> options,
        ILogger<TeamCartCouponSuggestionsOnItemQuantityUpdatedEventHandler> logger) : base(uow, inbox)
    {
        _couponSuggestionsService = couponSuggestionsService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemQuantityUpdatedInTeamCart notification, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _couponSuggestionsService.BroadcastCouponSuggestionsAsync(
            notification.TeamCartId, 
            notification.UpdatedByUserId, 
            "ItemQuantityUpdated", 
            ct);
    }
}
