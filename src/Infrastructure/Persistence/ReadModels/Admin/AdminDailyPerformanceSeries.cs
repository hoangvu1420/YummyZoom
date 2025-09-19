namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminDailyPerformanceSeries
{
    public DateOnly BucketDate { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal GrossMerchandiseVolume { get; set; }
    public decimal TotalRefunds { get; set; }
    public int NewCustomers { get; set; }
    public int NewRestaurants { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
