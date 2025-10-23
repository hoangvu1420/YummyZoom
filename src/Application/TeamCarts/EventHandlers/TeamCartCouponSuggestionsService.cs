using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Shared service for broadcasting TeamCart coupon suggestions via real-time notifications.
/// </summary>
public sealed class TeamCartCouponSuggestionsService
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IFastCouponCheckService _fastCouponCheckService;
    private readonly ITeamCartRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<TeamCartCouponSuggestionsService> _logger;

    public TeamCartCouponSuggestionsService(
        ITeamCartRepository teamCartRepository,
        IFastCouponCheckService fastCouponCheckService,
        ITeamCartRealtimeNotifier realtimeNotifier,
        ILogger<TeamCartCouponSuggestionsService> logger)
    {
        _teamCartRepository = teamCartRepository;
        _fastCouponCheckService = fastCouponCheckService;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task BroadcastCouponSuggestionsAsync(
        Domain.TeamCartAggregate.ValueObjects.TeamCartId teamCartId, 
        Domain.UserAggregate.ValueObjects.UserId userId,
        string eventType,
        CancellationToken ct)
    {
        try
        {
            // 1. Get current TeamCart state
            var teamCart = await _teamCartRepository.GetByIdAsync(teamCartId, ct);
            if (teamCart is null || !teamCart.Items.Any())
            {
                // No items or cart not found - broadcast empty suggestions
                await BroadcastEmptySuggestionsAsync(teamCartId, eventType, ct);
                return;
            }

            // 2. Convert TeamCart items to cart format
            var cartItems = teamCart.Items.Select(item => new CartItem(
                item.Snapshot_MenuItemId.Value,
                item.Snapshot_MenuCategoryId.Value,
                item.Quantity,
                item.Snapshot_BasePriceAtOrder.Amount,
                item.Snapshot_BasePriceAtOrder.Currency)).ToList();

            // 3. Get coupon suggestions
            var suggestions = await _fastCouponCheckService.GetSuggestionsAsync(
                teamCart.RestaurantId, cartItems, userId, ct);

            // 4. Broadcast lightweight coupon update
            var couponUpdate = new
            {
                Type = "CouponSuggestionsUpdated",
                EventType = eventType,
                CartSubtotal = suggestions.CartSummary.Subtotal,
                Currency = suggestions.CartSummary.Currency,
                BestDeal = suggestions.BestDeal != null ? new
                {
                    Code = suggestions.BestDeal.Code,
                    Label = suggestions.BestDeal.Label,
                    Savings = suggestions.BestDeal.Savings,
                    ExpiresOn = suggestions.BestDeal.ExpiresOn,
                    Urgency = suggestions.BestDeal.Urgency.ToString()
                } : null,
                SuggestionCount = suggestions.Suggestions.Count,
                UpdatedAt = DateTime.UtcNow
            };

            // Use existing NotifyCartUpdated with custom payload (implementation dependent)
            await _realtimeNotifier.NotifyCartUpdated(teamCartId, ct);

            _logger.LogInformation("Broadcasted coupon suggestions for TeamCart {TeamCartId} after {EventType}. " +
                                 "Best savings: {BestSavings}, Suggestions: {SuggestionCount}",
                teamCartId.Value, eventType, suggestions.BestDeal?.Savings ?? 0, suggestions.Suggestions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast coupon suggestions for TeamCart {TeamCartId} after {EventType}",
                teamCartId.Value, eventType);
            // Don't throw - this is a nice-to-have feature, shouldn't break the main flow
        }
    }

    private async Task BroadcastEmptySuggestionsAsync(
        Domain.TeamCartAggregate.ValueObjects.TeamCartId teamCartId,
        string eventType,
        CancellationToken ct)
    {
        try
        {
            var emptyUpdate = new
            {
                Type = "CouponSuggestionsUpdated",
                EventType = eventType,
                CartSubtotal = 0m,
                Currency = "USD",
                BestDeal = (object?)null,
                SuggestionCount = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _realtimeNotifier.NotifyCartUpdated(teamCartId, ct);

            _logger.LogDebug("Broadcasted empty coupon suggestions for TeamCart {TeamCartId} after {EventType}",
                teamCartId.Value, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast empty coupon suggestions for TeamCart {TeamCartId} after {EventType}",
                teamCartId.Value, eventType);
        }
    }
}
