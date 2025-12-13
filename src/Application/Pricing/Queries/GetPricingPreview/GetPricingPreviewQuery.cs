using Microsoft.AspNetCore.Authorization;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Pricing.Queries.GetPricingPreview;

[Authorize]
public record GetPricingPreviewQuery(
    Guid RestaurantId,
    List<PricingPreviewItemDto> Items,
    string? CouponCode = null,
    decimal? TipAmount = null,
    bool IncludeCouponSuggestions = false
) : IRequest<Result<GetPricingPreviewResponse>>, ICacheableQuery<GetPricingPreviewResponse>
{
    public string CacheKey => IncludeCouponSuggestions
        ? string.Empty
        : $"pricing:preview:v1:{RestaurantId:N}:{HashItemsAndCustomizations(Items)}";

    public CachePolicy Policy => CachePolicy.WithTtl(
        TimeSpan.FromMinutes(2), 
        $"restaurant:{RestaurantId:N}:menu");

    private static string HashItemsAndCustomizations(List<PricingPreviewItemDto> items)
    {
        var itemsHash = string.Join("|", items.OrderBy(i => i.MenuItemId).Select(i => 
            $"{i.MenuItemId}:{i.Quantity}:{string.Join(",", i.Customizations?.OrderBy(c => c.CustomizationGroupId).Select(c => 
                $"{c.CustomizationGroupId}:{string.Join(",", c.ChoiceIds.OrderBy(id => id))}") ?? new[] { "none" })}"));
        
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(itemsHash)))[..8];
    }
}

public record PricingPreviewItemDto(
    Guid MenuItemId,
    int Quantity,
    List<PricingPreviewCustomizationDto>? Customizations = null
);

public record PricingPreviewCustomizationDto(
    Guid CustomizationGroupId,
    List<Guid> ChoiceIds
);

public record GetPricingPreviewResponse(
    Money Subtotal,
    Money? DiscountAmount,
    Money DeliveryFee,
    Money TipAmount,
    Money TaxAmount,
    Money TotalAmount,
    string Currency,
    List<PricingPreviewNoteDto> Notes,
    DateTime CalculatedAt,
    CouponSuggestionsResponse? CouponSuggestions
);

public record PricingPreviewNoteDto(
    string Type, // "info", "warning", "error"
    string Code,
    string Message,
    Dictionary<string, object>? Metadata = null
);

public static class PricingPreviewErrors
{
    public static readonly Error RestaurantNotFoundOrInactive = Error.NotFound(
        "PricingPreview.RestaurantNotFound",
        "Restaurant not found or inactive");

    public static readonly Error NoValidItems = Error.Validation(
        "PricingPreview.NoValidItems",
        "No valid items found for pricing calculation");

    public static readonly Error MenuItemNotFound = Error.NotFound(
        "PricingPreview.MenuItemNotFound",
        "Menu item not found");

    public static readonly Error MenuItemUnavailable = Error.Validation(
        "PricingPreview.MenuItemUnavailable",
        "Menu item is currently unavailable");

    public static readonly Error CustomizationInvalid = Error.Validation(
        "PricingPreview.CustomizationInvalid",
        "Invalid customization selection");

    public static readonly Error CouponInvalid = Error.Validation(
        "PricingPreview.CouponInvalid",
        "Coupon is invalid or cannot be applied");

    public static readonly Error CouponNotFound = Error.NotFound(
        "PricingPreview.CouponNotFound",
        "Coupon not found");
}
