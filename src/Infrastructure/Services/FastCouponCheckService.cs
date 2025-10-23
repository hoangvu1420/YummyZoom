using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Services;

/// <summary>
/// Fast coupon check service using materialized view for optimized performance.
/// </summary>
public sealed class FastCouponCheckService : IFastCouponCheckService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly OrderFinancialService _orderFinancialService;
    private readonly ILogger<FastCouponCheckService> _logger;

    public FastCouponCheckService(
        IDbConnectionFactory dbConnectionFactory,
        OrderFinancialService orderFinancialService,
        ILogger<FastCouponCheckService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _orderFinancialService = orderFinancialService;
        _logger = logger;
    }

    public async Task<CouponSuggestionsResponse> GetSuggestionsAsync(
        RestaurantId restaurantId,
        IReadOnlyList<CartItem> cartItems,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        if (!cartItems.Any())
        {
            return CouponSuggestionsResponse.Empty();
        }

        try
        {
            // 1. Calculate cart facts once
            var cartFacts = CalculateCartFacts(cartItems);

            // 2. Load active coupons from materialized view (single query)
            var activeCoupons = await LoadActiveCouponsAsync(restaurantId, cancellationToken);

            if (!activeCoupons.Any())
            {
                return new CouponSuggestionsResponse(cartFacts.Summary, null, Array.Empty<CouponSuggestion>());
            }

            // 3. Load user usage counts (single query)
            var userUsages = await LoadUserUsageCountsAsync(userId, activeCoupons.Select(c => c.CouponId), cancellationToken);

            // 4. Calculate suggestions (in-memory, fast)
            var suggestions = new List<CouponSuggestion>();

            foreach (var coupon in activeCoupons)
            {
                var suggestion = await EvaluateCouponAsync(coupon, cartFacts, userUsages, cancellationToken);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }

            // 5. Sort by best deal first
            var sortedSuggestions = suggestions
                .OrderByDescending(s => s.IsEligible)
                .ThenByDescending(s => s.Savings)
                .ThenBy(s => s.ExpiresOn)
                .ThenBy(s => GetScopeRank(s.Scope))
                .ToList();

            var bestDeal = sortedSuggestions.FirstOrDefault(s => s.IsEligible && s.Savings > 0);

            return new CouponSuggestionsResponse(cartFacts.Summary, bestDeal, sortedSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupon suggestions for restaurant {RestaurantId}, user {UserId}", 
                restaurantId.Value, userId.Value);
            return CouponSuggestionsResponse.Empty();
        }
    }

    private static CartFacts CalculateCartFacts(IReadOnlyList<CartItem> cartItems)
    {
        var subtotal = cartItems.Sum(i => i.UnitPrice * i.Quantity);
        var currency = cartItems.First().Currency;
        var itemCount = cartItems.Sum(i => i.Quantity);

        var itemSubtotals = cartItems
            .GroupBy(i => i.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.UnitPrice * i.Quantity));

        var categorySubtotals = cartItems
            .GroupBy(i => i.MenuCategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.UnitPrice * i.Quantity));

        return new CartFacts(
            new CartSummary(subtotal, currency, itemCount),
            itemSubtotals,
            categorySubtotals);
    }

    private async Task<List<ActiveCouponView>> LoadActiveCouponsAsync(RestaurantId restaurantId, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                coupon_id AS CouponId,
                restaurant_id AS RestaurantId,
                code AS Code,
                description AS Description,
                value_type AS ValueType,
                percentage_value AS PercentageValue,
                fixed_amount_value AS FixedAmountValue,
                fixed_amount_currency AS FixedAmountCurrency,
                free_item_id AS FreeItemId,
                applies_to_scope AS AppliesToScope,
                applies_to_item_ids AS AppliesToItemIds,
                applies_to_category_ids AS AppliesToCategoryIds,
                min_order_amount AS MinOrderAmount,
                min_order_currency AS MinOrderCurrency,
                validity_start_date AS ValidityStartDate,
                validity_end_date AS ValidityEndDate,
                is_enabled AS IsEnabled,
                total_usage_limit AS TotalUsageLimit,
                usage_limit_per_user AS UsageLimitPerUser,
                current_total_usage_count AS CurrentTotalUsageCount,
                last_refreshed_at AS LastRefreshedAt
            FROM active_coupons_view 
            WHERE restaurant_id = @RestaurantId
              AND validity_start_date <= NOW()
              AND validity_end_date >= NOW()
            ORDER BY validity_end_date ASC, code ASC";

        var results = await connection.QueryAsync<ActiveCouponView>(sql, new { RestaurantId = restaurantId.Value });
        return results.ToList();
    }

    private async Task<Dictionary<Guid, int>> LoadUserUsageCountsAsync(UserId userId, IEnumerable<Guid> couponIds, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT ""CouponId"", ""UsageCount"" 
            FROM ""CouponUserUsages"" 
            WHERE ""UserId"" = @UserId AND ""CouponId"" = ANY(@CouponIds)";

        var results = await connection.QueryAsync<(Guid CouponId, int UsageCount)>(sql,
            new { UserId = userId.Value, CouponIds = couponIds.ToArray() });

        return results.ToDictionary(r => r.CouponId, r => r.UsageCount);
    }

    private Task<CouponSuggestion?> EvaluateCouponAsync(
        ActiveCouponView couponView,
        CartFacts cartFacts,
        Dictionary<Guid, int> userUsages,
        CancellationToken cancellationToken)
    {
        // 1. Check basic eligibility (time, enabled, usage limits)
        if (!IsBasicallyEligible(couponView, userUsages))
        {
            return Task.FromResult<CouponSuggestion?>(CreateIneligibleSuggestion(couponView, "UsageLimitExceeded", cartFacts.Summary.Subtotal));
        }

        // 2. Convert to domain coupon for calculation consistency
        var domainCoupon = MapToDomainCoupon(couponView);
        if (domainCoupon is null)
        {
            return Task.FromResult<CouponSuggestion?>(null); // Skip malformed coupons
        }

        // 3. Convert cart items to order items for calculation
        var orderItems = CreateOrderItemsFromCart(cartFacts);
        var subtotal = new Money(cartFacts.Summary.Subtotal, cartFacts.Summary.Currency);

        // 4. Use existing OrderFinancialService for consistency
        var discountResult = _orderFinancialService.ValidateAndCalculateDiscount(
            domainCoupon, orderItems, subtotal, DateTime.UtcNow);

        // 5. Create suggestion based on result
        if (discountResult.IsFailure)
        {
            var reason = MapErrorToReason(discountResult.Error);
            var minOrderGap = CalculateMinOrderGap(couponView, cartFacts.Summary.Subtotal);
            return Task.FromResult<CouponSuggestion?>(CreateIneligibleSuggestion(couponView, reason, cartFacts.Summary.Subtotal, minOrderGap));
        }

        var savings = Math.Round(discountResult.Value.Amount, 2);
        var urgency = CalculateUrgency(couponView);

        return Task.FromResult<CouponSuggestion?>(new CouponSuggestion(
            Code: couponView.Code,
            Label: CreateLabel(couponView),
            Savings: savings,
            IsEligible: true,
            EligibilityReason: null,
            MinOrderGap: 0,
            ExpiresOn: couponView.ValidityEndDate,
            Scope: couponView.AppliesToScope,
            Urgency: urgency));
    }

    private static bool IsBasicallyEligible(ActiveCouponView coupon, Dictionary<Guid, int> userUsages)
    {
        // Check total usage limit
        if (coupon.TotalUsageLimit.HasValue && coupon.CurrentTotalUsageCount >= coupon.TotalUsageLimit.Value)
        {
            return false;
        }

        // Check per-user usage limit
        if (coupon.UsageLimitPerUser.HasValue)
        {
            var userUsageCount = userUsages.GetValueOrDefault(coupon.CouponId, 0);
            if (userUsageCount >= coupon.UsageLimitPerUser.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static Coupon? MapToDomainCoupon(ActiveCouponView view)
    {
        try
        {
            // Parse coupon value
            var couponValue = view.ValueType switch
            {
                "Percentage" => CouponValue.CreatePercentage(view.PercentageValue ?? 0),
                "FixedAmount" => CouponValue.CreateFixedAmount(new Money(view.FixedAmountValue ?? 0, view.FixedAmountCurrency ?? "USD")),
                "FreeItem" => CouponValue.CreateFreeItem(MenuItemId.Create(view.FreeItemId ?? Guid.Empty)),
                _ => null
            };

            if (couponValue?.IsSuccess != true)
            {
                return null;
            }

            // Parse applies to
            var itemIds = string.IsNullOrEmpty(view.AppliesToItemIds) 
                ? new List<MenuItemId>() 
                : JsonSerializer.Deserialize<List<Guid>>(view.AppliesToItemIds)?.Select(MenuItemId.Create).ToList() ?? new List<MenuItemId>();

            var categoryIds = string.IsNullOrEmpty(view.AppliesToCategoryIds) 
                ? new List<MenuCategoryId>() 
                : JsonSerializer.Deserialize<List<Guid>>(view.AppliesToCategoryIds)?.Select(MenuCategoryId.Create).ToList() ?? new List<MenuCategoryId>();

            var scope = Enum.Parse<CouponScope>(view.AppliesToScope);
            var appliesTo = scope switch
            {
                CouponScope.WholeOrder => AppliesTo.CreateForWholeOrder(),
                CouponScope.SpecificItems => AppliesTo.CreateForSpecificItems(itemIds),
                CouponScope.SpecificCategories => AppliesTo.CreateForSpecificCategories(categoryIds),
                _ => Result.Failure<AppliesTo>(Error.Failure("InvalidScope", "Invalid coupon scope"))
            };

            if (appliesTo.IsFailure)
            {
                return null;
            }

            // Create coupon
            var minOrderAmount = view.MinOrderAmount.HasValue 
                ? new Money(view.MinOrderAmount.Value, view.MinOrderCurrency ?? "USD") 
                : null;

            var couponResult = Coupon.Create(
                RestaurantId.Create(view.RestaurantId),
                view.Code,
                view.Description,
                couponValue.Value,
                appliesTo.Value,
                view.ValidityStartDate,
                view.ValidityEndDate,
                minOrderAmount,
                view.TotalUsageLimit,
                view.UsageLimitPerUser,
                view.IsEnabled);
                
            return couponResult.IsSuccess ? couponResult.Value : null;
        }
        catch
        {
            return null; // Skip malformed coupons
        }
    }

    private static List<OrderItem> CreateOrderItemsFromCart(CartFacts cartFacts)
    {
        var orderItems = new List<OrderItem>();
        var currency = cartFacts.Summary.Currency;

        foreach (var (itemId, subtotal) in cartFacts.ItemSubtotals)
        {
            // Create a simplified order item for calculation
            var createResult = OrderItem.Create(
                MenuCategoryId.Create(Guid.Empty), // We'll use category mapping separately
                MenuItemId.Create(itemId),
                "Cart Item",
                new Money(subtotal, currency),
                1, // Quantity is already factored into subtotal
                null);

            if (createResult.IsSuccess)
            {
                orderItems.Add(createResult.Value);
            }
        }

        return orderItems;
    }

    private static decimal CalculateMinOrderGap(ActiveCouponView coupon, decimal cartSubtotal)
    {
        if (!coupon.MinOrderAmount.HasValue)
        {
            return 0;
        }

        return Math.Max(0, coupon.MinOrderAmount.Value - cartSubtotal);
    }

    private static string MapErrorToReason(SharedKernel.Error error)
    {
        return error.Code switch
        {
            var code when code.Contains("MinAmountNotMet") => "MinAmountNotMet",
            var code when code.Contains("Expired") => "Expired",
            var code when code.Contains("NotYetValid") => "NotYetValid",
            var code when code.Contains("Disabled") => "Disabled",
            var code when code.Contains("NotApplicable") => "NotApplicable",
            _ => "Other"
        };
    }

    private static CouponSuggestion CreateIneligibleSuggestion(ActiveCouponView coupon, string reason, decimal cartSubtotal, decimal minOrderGap = 0)
    {
        return new CouponSuggestion(
            Code: coupon.Code,
            Label: CreateLabel(coupon),
            Savings: 0,
            IsEligible: false,
            EligibilityReason: reason,
            MinOrderGap: minOrderGap,
            ExpiresOn: coupon.ValidityEndDate,
            Scope: coupon.AppliesToScope);
    }

    private static string CreateLabel(ActiveCouponView coupon)
    {
        return coupon.ValueType switch
        {
            "Percentage" => $"{coupon.PercentageValue}% off",
            "FixedAmount" => $"{coupon.FixedAmountValue} {coupon.FixedAmountCurrency} off",
            "FreeItem" => "Free item",
            _ => coupon.Description
        };
    }

    private static CouponUrgency CalculateUrgency(ActiveCouponView coupon)
    {
        var timeToExpiry = coupon.ValidityEndDate - DateTime.UtcNow;

        if (timeToExpiry.TotalHours <= 24)
        {
            return CouponUrgency.ExpiresWithin24Hours;
        }

        if (timeToExpiry.TotalDays <= 7)
        {
            return CouponUrgency.ExpiresWithin7Days;
        }

        if (coupon.TotalUsageLimit.HasValue)
        {
            var remainingUses = coupon.TotalUsageLimit.Value - coupon.CurrentTotalUsageCount;
            if (remainingUses <= 10) // Arbitrary threshold
            {
                return CouponUrgency.LimitedUsesRemaining;
            }
        }

        return CouponUrgency.None;
    }

    private static int GetScopeRank(string scope)
    {
        return scope switch
        {
            nameof(CouponScope.WholeOrder) => 0,
            nameof(CouponScope.SpecificCategories) => 1,
            nameof(CouponScope.SpecificItems) => 2,
            _ => 3
        };
    }

    private sealed record CartFacts(
        CartSummary Summary,
        Dictionary<Guid, decimal> ItemSubtotals,
        Dictionary<Guid, decimal> CategorySubtotals);
}
