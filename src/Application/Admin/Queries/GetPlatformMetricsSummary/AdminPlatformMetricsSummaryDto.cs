namespace YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;

/// <summary>
/// Data transfer object representing the top-line admin platform metrics snapshot.
/// </summary>
public sealed record AdminPlatformMetricsSummaryDto(
    long TotalOrders,
    long ActiveOrders,
    long DeliveredOrders,
    decimal GrossMerchandiseVolume,
    decimal TotalRefunds,
    int ActiveRestaurants,
    int ActiveCustomers,
    int OpenSupportTickets,
    int TotalReviews,
    DateTime? LastOrderAtUtc,
    DateTime UpdatedAtUtc);
