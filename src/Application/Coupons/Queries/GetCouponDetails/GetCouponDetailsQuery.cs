using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Queries.GetCouponDetails;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetCouponDetailsQuery(Guid RestaurantId, Guid CouponId)
    : IRequest<Result<CouponDetailsDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class GetCouponDetailsQueryValidator : AbstractValidator<GetCouponDetailsQuery>
{
    public GetCouponDetailsQueryValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.CouponId).NotEmpty();
    }
}

public sealed record CouponDetailsDto(
    Guid CouponId,
    string Code,
    string Description,
    CouponType ValueType,
    decimal? Percentage,
    decimal? FixedAmount,
    string? FixedCurrency,
    Guid? FreeItemId,
    CouponScope Scope,
    IReadOnlyList<Guid> ItemIds,
    IReadOnlyList<Guid> CategoryIds,
    DateTime ValidityStartDate,
    DateTime ValidityEndDate,
    decimal? MinOrderAmount,
    string? MinOrderCurrency,
    int? TotalUsageLimit,
    int CurrentTotalUsageCount,
    int? UsageLimitPerUser,
    bool IsEnabled,
    DateTimeOffset Created,
    DateTimeOffset LastModified);

public static class GetCouponDetailsErrors
{
    public static Error NotFound(Guid couponId) => Error.NotFound(
        "Coupon.Details.NotFound",
        $"Coupon with ID '{couponId}' was not found.");
}
