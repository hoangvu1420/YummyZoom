using System.Text.Json.Serialization;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Bundles;

/// <summary>
/// Represents a restaurant's coupon bundle loaded from a JSON file for seeding purposes.
/// File naming convention: {restaurant-slug}.coupon.json
/// </summary>
public sealed class CouponBundle
{
    /// <summary>
    /// The slug of the restaurant these coupons belong to.
    /// Used to resolve the restaurant ID during seeding.
    /// Example: "pho-gia-truyen-bat-dan", "bun-cha-huong-lien"
    /// </summary>
    [JsonPropertyName("restaurantSlug")]
    public string RestaurantSlug { get; set; } = string.Empty;

    /// <summary>
    /// Array of coupons for this restaurant.
    /// </summary>
    [JsonPropertyName("coupons")]
    public List<CouponData> Coupons { get; set; } = new();
}

/// <summary>
/// Represents a single coupon's data within a bundle.
/// </summary>
public sealed class CouponData
{
    /// <summary>
    /// Unique coupon code that customers will enter.
    /// Will be normalized to uppercase during creation.
    /// Max length: 50 characters.
    /// Example: "GIAMGIA20", "FREESHIP"
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the coupon in Vietnamese.
    /// Example: "Giảm 20% cho đơn hàng trên 200.000đ"
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of discount value.
    /// Valid values: "Percentage", "FixedAmount", "FreeItem"
    /// </summary>
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    /// Percentage value (1-100) if ValueType is "Percentage".
    /// Example: 20 for 20% off
    /// </summary>
    [JsonPropertyName("percentage")]
    public decimal? Percentage { get; set; }

    /// <summary>
    /// Fixed discount amount if ValueType is "FixedAmount".
    /// Example: 50000 for 50,000 VND off
    /// </summary>
    [JsonPropertyName("fixedAmount")]
    public decimal? FixedAmount { get; set; }

    /// <summary>
    /// Currency code for fixed amount (e.g., "VND", "USD").
    /// Required if ValueType is "FixedAmount".
    /// </summary>
    [JsonPropertyName("fixedCurrency")]
    public string? FixedCurrency { get; set; }

    /// <summary>
    /// Name of the free menu item if ValueType is "FreeItem".
    /// Will be resolved to MenuItemId by name lookup during seeding.
    /// Example: "Chả giò"
    /// </summary>
    [JsonPropertyName("freeItemName")]
    public string? FreeItemName { get; set; }

    /// <summary>
    /// Scope of the coupon application.
    /// Valid values: "WholeOrder", "SpecificItems", "SpecificCategories"
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "WholeOrder";

    /// <summary>
    /// Names of specific menu items the coupon applies to.
    /// Only used if Scope is "SpecificItems".
    /// Items will be resolved by name lookup during seeding.
    /// Example: ["Phở bò", "Phở gà"]
    /// </summary>
    [JsonPropertyName("itemNames")]
    public List<string>? ItemNames { get; set; }

    /// <summary>
    /// Names of specific menu categories the coupon applies to.
    /// Only used if Scope is "SpecificCategories".
    /// Categories will be resolved by name lookup during seeding.
    /// Example: ["Món chính", "Món khai vị"]
    /// </summary>
    [JsonPropertyName("categoryNames")]
    public List<string>? CategoryNames { get; set; }

    /// <summary>
    /// When the coupon becomes valid for use (UTC).
    /// Example: "2025-11-01T00:00:00Z"
    /// </summary>
    [JsonPropertyName("validityStartDate")]
    public DateTime ValidityStartDate { get; set; }

    /// <summary>
    /// When the coupon expires (UTC).
    /// Example: "2025-12-31T23:59:59Z"
    /// </summary>
    [JsonPropertyName("validityEndDate")]
    public DateTime ValidityEndDate { get; set; }

    /// <summary>
    /// Minimum order amount required to use the coupon.
    /// Example: 200000 for 200,000 VND
    /// </summary>
    [JsonPropertyName("minOrderAmount")]
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Currency code for minimum order amount (e.g., "VND", "USD").
    /// Required if MinOrderAmount is specified.
    /// </summary>
    [JsonPropertyName("minOrderCurrency")]
    public string? MinOrderCurrency { get; set; }

    /// <summary>
    /// Global usage limit across all users.
    /// Example: 1000 for maximum 1000 uses
    /// Null means unlimited.
    /// </summary>
    [JsonPropertyName("totalUsageLimit")]
    public int? TotalUsageLimit { get; set; }

    /// <summary>
    /// Per-user usage limit.
    /// Example: 1 for one use per customer
    /// Null means unlimited per user.
    /// </summary>
    [JsonPropertyName("usageLimitPerUser")]
    public int? UsageLimitPerUser { get; set; }

    /// <summary>
    /// Whether the coupon is initially enabled.
    /// Default: true
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}

