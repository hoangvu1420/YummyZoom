namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminPlatformMetricsSnapshot
{
    public string SnapshotId { get; set; } = default!;
    public long TotalOrders { get; set; }
    public long ActiveOrders { get; set; }
    public long DeliveredOrders { get; set; }
    public decimal GrossMerchandiseVolume { get; set; }
    public decimal TotalRefunds { get; set; }
    public int ActiveRestaurants { get; set; }
    public int ActiveCustomers { get; set; }
    public int OpenSupportTickets { get; set; }
    public int TotalReviews { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
