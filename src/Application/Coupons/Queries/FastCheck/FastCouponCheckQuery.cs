using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed record FastCouponCheckQuery(
    Guid RestaurantId,
    IReadOnlyList<FastCouponCheckItemDto> Items,
    decimal? TipAmount = null
) : IRequest<Result<CouponSuggestionsResponse>>
{
    public bool IsValid => RestaurantId != Guid.Empty && Items.Any() && Items.All(i => i.IsValid);
}

public sealed record FastCouponCheckItemDto(
    Guid MenuItemId,
    int Quantity,
    List<FastCouponCheckCustomizationDto>? Customizations = null)
{
    public bool IsValid => MenuItemId != Guid.Empty && Quantity > 0;
}

public sealed record FastCouponCheckCustomizationDto(
    Guid CustomizationGroupId,
    List<Guid> ChoiceIds);
