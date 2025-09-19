using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Queries.GetCouponStats;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetCouponStatsQuery(Guid RestaurantId, Guid CouponId)
    : IRequest<Result<CouponStatsDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class GetCouponStatsQueryValidator : AbstractValidator<GetCouponStatsQuery>
{
    public GetCouponStatsQueryValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.CouponId).NotEmpty();
    }
}

public sealed record CouponStatsDto(
    int TotalUsage,
    int UniqueUsers,
    DateTime? LastUsedAtUtc);

public static class GetCouponStatsErrors
{
    public static Error NotFound(Guid couponId) => Error.NotFound(
        "Coupon.Stats.NotFound",
        $"Coupon with ID '{couponId}' was not found.");
}
