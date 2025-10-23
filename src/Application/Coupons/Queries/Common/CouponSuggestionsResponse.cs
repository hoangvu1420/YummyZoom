namespace YummyZoom.Application.Coupons.Queries.Common;

/// <summary>
/// Response containing coupon suggestions with calculated savings and eligibility.
/// </summary>
public sealed record CouponSuggestionsResponse(
    CartSummary CartSummary,
    CouponSuggestion? BestDeal,
    IReadOnlyList<CouponSuggestion> Suggestions)
{
    public static CouponSuggestionsResponse Empty() => new(
        new CartSummary(0, "USD", 0),
        null,
        Array.Empty<CouponSuggestion>());
}

/// <summary>
/// Summary of the cart for which coupons are being suggested.
/// </summary>
public sealed record CartSummary(
    decimal Subtotal,
    string Currency,
    int ItemCount);

/// <summary>
/// A coupon suggestion with calculated savings and eligibility information.
/// </summary>
public sealed record CouponSuggestion(
    string Code,
    string Label,
    decimal Savings,
    bool IsEligible,
    string? EligibilityReason,
    decimal MinOrderGap,
    DateTime ExpiresOn,
    string Scope,
    CouponUrgency Urgency = CouponUrgency.None);

/// <summary>
/// Indicates the urgency of applying a coupon (e.g., expires soon).
/// </summary>
public enum CouponUrgency
{
    None,
    ExpiresWithin24Hours,
    ExpiresWithin7Days,
    LimitedUsesRemaining
}
