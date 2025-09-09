namespace YummyZoom.Infrastructure.Persistence.EfCore.Models;

public class CouponUserUsage
{
    public Guid CouponId { get; set; }
    public Guid UserId { get; set; }
    public int UsageCount { get; set; }
}
