using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed record FastCouponCheckItemDto(
    Guid MenuItemId,
    Guid MenuCategoryId,
    int Qty,
    decimal UnitPrice,
    string Currency = "USD")
{
    public bool IsValid => MenuItemId != Guid.Empty && MenuCategoryId != Guid.Empty && Qty > 0 && UnitPrice >= 0;
}

public sealed record FastCouponCheckQuery(
    Guid RestaurantId,
    IReadOnlyList<FastCouponCheckItemDto> Items
) : IRequest<Result<CouponSuggestionsResponse>>
{
    public bool IsValid => RestaurantId != Guid.Empty && Items.Any() && Items.All(i => i.IsValid);
}

