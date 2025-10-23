using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCart item events to optionally broadcast updated coupon suggestions.
/// This provides real-time coupon recommendations as users add/remove items from collaborative carts.
/// </summary>
public sealed class TeamCartCouponSuggestionsEventHandler : IdempotentNotificationHandler<ItemAddedToTeamCart>
{
    private readonly TeamCartCouponSuggestionsService _couponSuggestionsService;
    private readonly TeamCartCouponSuggestionsOptions _options;
    private readonly ILogger<TeamCartCouponSuggestionsEventHandler> _logger;

    public TeamCartCouponSuggestionsEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        TeamCartCouponSuggestionsService couponSuggestionsService,
        IOptions<TeamCartCouponSuggestionsOptions> options,
        ILogger<TeamCartCouponSuggestionsEventHandler> logger) : base(uow, inbox)
    {
        _couponSuggestionsService = couponSuggestionsService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemAddedToTeamCart notification, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _couponSuggestionsService.BroadcastCouponSuggestionsAsync(
            notification.TeamCartId, 
            notification.AddedByUserId, 
            "ItemAdded", 
            ct);
    }
}

/// <summary>
/// Configuration options for TeamCart coupon suggestions real-time notifications.
/// </summary>
public sealed class TeamCartCouponSuggestionsOptions
{
    public const string SectionName = "TeamCartCouponSuggestions";
    
    /// <summary>
    /// Whether to enable real-time coupon suggestion broadcasts when TeamCart items change.
    /// </summary>
    public bool Enabled { get; set; } = false; // Disabled by default for MVP
    
    /// <summary>
    /// Minimum delay between broadcasts to prevent spam (in milliseconds).
    /// </summary>
    public int ThrottleMs { get; set; } = 1000;
}
