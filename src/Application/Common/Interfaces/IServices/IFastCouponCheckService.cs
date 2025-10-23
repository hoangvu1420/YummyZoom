using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Service for fast coupon eligibility checking and savings calculation.
/// Provides optimized queries for coupon recommendations without complex domain logic.
/// </summary>
public interface IFastCouponCheckService
{
    /// <summary>
    /// Gets coupon suggestions for a cart with calculated savings and eligibility.
    /// </summary>
    /// <param name="restaurantId">The restaurant ID</param>
    /// <param name="cartItems">The items in the cart</param>
    /// <param name="userId">The user ID for usage limit checks</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Coupon suggestions response with best deal and all applicable coupons</returns>
    Task<CouponSuggestionsResponse> GetSuggestionsAsync(
        RestaurantId restaurantId,
        IReadOnlyList<CartItem> cartItems,
        UserId userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an item in a cart for coupon checking.
/// </summary>
public record CartItem(
    Guid MenuItemId,
    Guid MenuCategoryId,
    int Quantity,
    decimal UnitPrice,
    string Currency = "USD");
