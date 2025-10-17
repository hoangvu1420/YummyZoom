namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

/// <summary>
/// Configuration options for the Order seeder.
/// </summary>
public sealed class OrderSeedingOptions
{
    /// <summary>
    /// Number of orders to create per restaurant.
    /// </summary>
    public int OrdersPerRestaurant { get; set; } = 10;

    /// <summary>
    /// Distribution of order statuses as percentages (must sum to 100).
    /// Key is the OrderStatus name, value is the percentage (0-100).
    /// Example: { "Delivered": 60, "Accepted": 10, "Preparing": 10, "ReadyForDelivery": 5, "Cancelled": 10, "Rejected": 5 }
    /// </summary>
    public Dictionary<string, int> StatusDistribution { get; set; } = new()
    {
        { "Delivered", 60 },
        { "Accepted", 10 },
        { "Preparing", 10 },
        { "ReadyForDelivery", 5 },
        { "Cancelled", 10 },
        { "Rejected", 5 }
    };

    /// <summary>
    /// Percentage of orders that should use coupons (0-100).
    /// </summary>
    public decimal CouponUsagePercentage { get; set; } = 30;

    /// <summary>
    /// Percentage of orders that should use online payment vs Cash on Delivery (0-100).
    /// </summary>
    public decimal OnlinePaymentPercentage { get; set; } = 70;

    /// <summary>
    /// When true, generates realistic timestamps spread over OrderHistoryDays.
    /// When false, all orders are created with current timestamp.
    /// </summary>
    public bool CreateRealisticTimestamps { get; set; } = true;

    /// <summary>
    /// Number of days in the past to spread order timestamps over.
    /// Only used if CreateRealisticTimestamps is true.
    /// </summary>
    public int OrderHistoryDays { get; set; } = 90;

    /// <summary>
    /// When true, adds random special instructions to some orders.
    /// </summary>
    public bool GenerateSpecialInstructions { get; set; } = true;

    /// <summary>
    /// Percentage of orders that should include a tip (0-100).
    /// </summary>
    public decimal TipPercentage { get; set; } = 40;

    /// <summary>
    /// Minimum number of items per order.
    /// </summary>
    public int MinItemsPerOrder { get; set; } = 1;

    /// <summary>
    /// Maximum number of items per order.
    /// </summary>
    public int MaxItemsPerOrder { get; set; } = 5;
}
