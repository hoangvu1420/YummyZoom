using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed record FastCouponCheckItemDto(
    Guid MenuItemId,
    Guid MenuCategoryId,
    int Qty,
    decimal UnitPrice
);

public sealed record FastCouponCheckQuery(
    Guid RestaurantId,
    IReadOnlyList<FastCouponCheckItemDto> Items
) : IRequest<Result<FastCouponCheckResponse>>;

public sealed record FastCouponCandidateDto(
    string Code,
    string Label,
    decimal Savings,
    bool MeetsMinOrder,
    decimal MinOrderGap,
    DateTime ValidityEnd,
    string Scope,
    string? ReasonIfIneligible
);

public sealed record FastCouponCheckResponse(
    FastCouponCandidateDto? BestDeal,
    IReadOnlyList<FastCouponCandidateDto> Candidates
);

