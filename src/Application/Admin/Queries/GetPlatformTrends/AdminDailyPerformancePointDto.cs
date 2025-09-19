namespace YummyZoom.Application.Admin.Queries.GetPlatformTrends;

/// <summary>
/// Represents a single bucket in the platform performance time series.
/// </summary>
public sealed record AdminDailyPerformancePointDto(
    DateOnly BucketDate,
    int TotalOrders,
    int DeliveredOrders,
    decimal GrossMerchandiseVolume,
    decimal TotalRefunds,
    int NewCustomers,
    int NewRestaurants,
    DateTime UpdatedAtUtc);
