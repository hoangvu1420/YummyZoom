using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Queries.ListCouponsByRestaurant;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record ListCouponsByRestaurantQuery(
    Guid RestaurantId,
    int PageNumber = 1,
    int PageSize = 20,
    string? Q = null,
    bool? IsEnabled = null,
    DateTime? ActiveFrom = null,
    DateTime? ActiveTo = null)
    : IRequest<Result<PaginatedList<CouponSummaryDto>>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class ListCouponsByRestaurantQueryValidator : AbstractValidator<ListCouponsByRestaurantQuery>
{
    private const int MaxPageSize = 100;

    public ListCouponsByRestaurantQueryValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Q).MaximumLength(200);

        When(x => x.ActiveFrom.HasValue && x.ActiveTo.HasValue, () =>
        {
            RuleFor(x => x).Must(x => x.ActiveFrom <= x.ActiveTo)
                .WithMessage("ActiveFrom must be earlier than or equal to ActiveTo.");
        });
    }
}

public sealed record CouponSummaryDto(
    Guid CouponId,
    string Code,
    string Description,
    CouponType ValueType,
    decimal? Percentage,
    decimal? FixedAmount,
    string? FixedCurrency,
    Guid? FreeItemId,
    CouponScope Scope,
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
