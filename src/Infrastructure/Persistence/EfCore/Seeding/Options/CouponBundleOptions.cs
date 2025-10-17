namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;

/// <summary>
/// Configuration options for the CouponBundle seeder.
/// </summary>
public sealed class CouponBundleOptions
{
    /// <summary>
    /// When true, logs what would be done without making changes.
    /// </summary>
    public bool ReportOnly { get; set; } = false;

    /// <summary>
    /// When true, updates existing coupons with the same code instead of skipping them.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Glob patterns for coupon bundle files to load (e.g., "*.coupon.json").
    /// Patterns are matched against filenames in the Data/Coupons directory.
    /// </summary>
    public string[] CouponGlobs { get; set; } = new[] { "*.coupon.json" };
}
