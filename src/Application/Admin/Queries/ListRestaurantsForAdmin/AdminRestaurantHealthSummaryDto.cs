namespace YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;

/// <summary>
/// Projection returned to admin dashboard clients when listing restaurant health summaries.
/// </summary>
public sealed record AdminRestaurantHealthSummaryDto(
    Guid RestaurantId,
    string RestaurantName,
    bool IsVerified,
    bool IsAcceptingOrders,
    int OrdersLast7Days,
    int OrdersLast30Days,
    decimal RevenueLast30Days,
    double AverageRating,
    int TotalReviews,
    int CouponRedemptionsLast30Days,
    decimal OutstandingBalance,
    DateTime? LastOrderAtUtc,
    DateTime UpdatedAtUtc);
