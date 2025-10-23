using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed class FastCouponCheckQueryHandler : IRequestHandler<FastCouponCheckQuery, Result<CouponSuggestionsResponse>>
{
    private readonly IFastCouponCheckService _fastCouponCheckService;
    private readonly IUser _currentUser;
    private readonly ILogger<FastCouponCheckQueryHandler> _logger;

    public FastCouponCheckQueryHandler(
        IFastCouponCheckService fastCouponCheckService,
        IUser currentUser,
        ILogger<FastCouponCheckQueryHandler> logger)
    {
        _fastCouponCheckService = fastCouponCheckService ?? throw new ArgumentNullException(nameof(fastCouponCheckService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CouponSuggestionsResponse>> Handle(FastCouponCheckQuery request, CancellationToken ct)
    {
        if (_currentUser.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        try
        {
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var userId = _currentUser.DomainUserId;

            // Convert request items to CartItem format
            var cartItems = request.Items.Select(item => new CartItem(
                item.MenuItemId,
                item.MenuCategoryId,
                item.Qty,
                item.UnitPrice,
                item.Currency)).ToList();

            // Use the optimized fast coupon check service
            var suggestions = await _fastCouponCheckService.GetSuggestionsAsync(
                restaurantId, cartItems, userId, ct);

            _logger.LogInformation("Fast coupon check completed for restaurant {RestaurantId}, user {UserId}. " +
                                 "Cart subtotal: {Subtotal}, suggestions: {SuggestionCount}, best savings: {BestSavings}",
                restaurantId.Value, userId.Value, suggestions.CartSummary.Subtotal, 
                suggestions.Suggestions.Count, suggestions.BestDeal?.Savings ?? 0);

            return Result.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fast coupon check for restaurant {RestaurantId}", request.RestaurantId);
            return Result.Failure<CouponSuggestionsResponse>(Error.Failure("FastCouponCheck.ProcessingError", "An error occurred while processing coupon suggestions"));
        }
    }

}
