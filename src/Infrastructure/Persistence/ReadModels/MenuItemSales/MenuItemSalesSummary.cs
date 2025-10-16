namespace YummyZoom.Infrastructure.Persistence.ReadModels.MenuItemSales;

/// <summary>
/// Denormalized sales counters for a specific menu item.
/// </summary>
public class MenuItemSalesSummary
{
    public Guid RestaurantId { get; set; }
    public Guid MenuItemId { get; set; }

    /// <summary>
    /// Total delivered quantity recorded for the menu item.
    /// </summary>
    public long LifetimeQuantity { get; set; }

    /// <summary>
    /// Quantity delivered over the trailing 7-day window.
    /// </summary>
    public long Rolling7DayQuantity { get; set; }

    /// <summary>
    /// Quantity delivered over the trailing 30-day window.
    /// </summary>
    public long Rolling30DayQuantity { get; set; }

    /// <summary>
    /// Timestamp of the most recent delivered order that included the item.
    /// </summary>
    public DateTimeOffset? LastSoldAt { get; set; }

    /// <summary>
    /// Timestamp of the last update touching this summary.
    /// Used for cache invalidation.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Monotonic source version to guarantee idempotent upserts.
    /// </summary>
    public long SourceVersion { get; set; }
}
