namespace YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;

/// <summary>
/// DTO representing a row from the active_coupons_view materialized view.
/// Used for fast coupon check queries without complex joins.
/// </summary>
public class ActiveCouponView
{
    public Guid CouponId { get; set; }
    public Guid RestaurantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Value properties
    public string ValueType { get; set; } = string.Empty;
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmountValue { get; set; }
    public string? FixedAmountCurrency { get; set; }
    public Guid? FreeItemId { get; set; }
    
    // Applies to properties
    public string AppliesToScope { get; set; } = string.Empty;
    public string? AppliesToItemIds { get; set; } // JSON array
    public string? AppliesToCategoryIds { get; set; } // JSON array
    
    // Constraints
    public decimal? MinOrderAmount { get; set; }
    public string? MinOrderCurrency { get; set; }
    public DateTime ValidityStartDate { get; set; }
    public DateTime ValidityEndDate { get; set; }
    
    // Usage limits
    public bool IsEnabled { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public int CurrentTotalUsageCount { get; set; }
    
    // Metadata
    public DateTime LastRefreshedAt { get; set; }
}
