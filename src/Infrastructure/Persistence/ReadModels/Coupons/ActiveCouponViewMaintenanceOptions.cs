namespace YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;

public sealed class ActiveCouponViewMaintenanceOptions
{
    public const string SectionName = "ActiveCouponViewMaintenance";
    
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int LogEveryN { get; set; } = 10;
}
