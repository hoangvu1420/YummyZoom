namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminRestaurantHealthSummary
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsAcceptingOrders { get; set; }
    public int OrdersLast7Days { get; set; }
    public int OrdersLast30Days { get; set; }
    public decimal RevenueLast30Days { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int CouponRedemptionsLast30Days { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
