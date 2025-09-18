using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Commands.CreateCoupon;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record CreateCouponCommand(
    Guid RestaurantId,
    string Code,
    string Description,
    CouponType ValueType,
    decimal? Percentage,
    decimal? FixedAmount,
    string? FixedCurrency,
    Guid? FreeItemId,
    CouponScope Scope,
    List<Guid>? ItemIds,
    List<Guid>? CategoryIds,
    DateTime ValidityStartDate,
    DateTime ValidityEndDate,
    decimal? MinOrderAmount,
    string? MinOrderCurrency,
    int? TotalUsageLimit,
    int? UsageLimitPerUser,
    bool IsEnabled
) : IRequest<Result<CreateCouponResponse>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record CreateCouponResponse(Guid CouponId);

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ValidityStartDate).LessThan(x => x.ValidityEndDate);

        When(x => x.ValueType == CouponType.Percentage, () =>
        {
            RuleFor(x => x.Percentage).NotNull().GreaterThan(0).LessThanOrEqualTo(100);
        });

        When(x => x.ValueType == CouponType.FixedAmount, () =>
        {
            RuleFor(x => x.FixedAmount).NotNull().GreaterThan(0);
            RuleFor(x => x.FixedCurrency).NotEmpty();
        });

        When(x => x.ValueType == CouponType.FreeItem, () =>
        {
            RuleFor(x => x.FreeItemId).NotNull();
        });

        When(x => x.Scope == CouponScope.SpecificItems, () =>
        {
            RuleFor(x => x.ItemIds).NotNull().Must(ids => ids!.Count > 0).WithMessage("ItemIds must contain at least one id.");
        });

        When(x => x.Scope == CouponScope.SpecificCategories, () =>
        {
            RuleFor(x => x.CategoryIds).NotNull().Must(ids => ids!.Count > 0).WithMessage("CategoryIds must contain at least one id.");
        });

        When(x => x.MinOrderAmount.HasValue, () =>
        {
            RuleFor(x => x.MinOrderCurrency).NotEmpty();
            RuleFor(x => x.MinOrderAmount!.Value).GreaterThan(0);
        });

        When(x => x.TotalUsageLimit.HasValue, () =>
        {
            RuleFor(x => x.TotalUsageLimit!.Value).GreaterThan(0);
        });

        When(x => x.UsageLimitPerUser.HasValue, () =>
        {
            RuleFor(x => x.UsageLimitPerUser!.Value).GreaterThan(0);
        });
    }
}

public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<CreateCouponResponse>>
{
    private readonly ICouponRepository _coupons;
    private readonly IUnitOfWork _uow;

    public CreateCouponCommandHandler(ICouponRepository coupons, IUnitOfWork uow)
    {
        _coupons = coupons;
        _uow = uow;
    }

    public async Task<Result<CreateCouponResponse>> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            var rid = RestaurantId.Create(request.RestaurantId);

            // Build CouponValue
            Result<CouponValue> valueRes = request.ValueType switch
            {
                CouponType.Percentage => CouponValue.CreatePercentage(request.Percentage!.Value),
                CouponType.FixedAmount => CouponValue.CreateFixedAmount(new Money(request.FixedAmount!.Value, request.FixedCurrency!)),
                CouponType.FreeItem => CouponValue.CreateFreeItem(MenuItemId.Create(request.FreeItemId!.Value)),
                _ => Result.Failure<CouponValue>(Error.Validation("Coupon.ValueTypeInvalid", "Unsupported coupon value type."))
            };
            if (valueRes.IsFailure) return Result.Failure<CreateCouponResponse>(valueRes.Error);

            // Build AppliesTo
            Result<AppliesTo> appliesRes = request.Scope switch
            {
                CouponScope.WholeOrder => AppliesTo.CreateForWholeOrder(),
                CouponScope.SpecificItems => AppliesTo.CreateForSpecificItems(request.ItemIds!.Select(MenuItemId.Create).ToList()),
                CouponScope.SpecificCategories => AppliesTo.CreateForSpecificCategories(request.CategoryIds!.Select(MenuCategoryId.Create).ToList()),
                _ => Result.Failure<AppliesTo>(Error.Validation("Coupon.ScopeInvalid", "Unsupported coupon scope."))
            };
            if (appliesRes.IsFailure) return Result.Failure<CreateCouponResponse>(appliesRes.Error);

            Money? minOrder = null;
            if (request.MinOrderAmount.HasValue)
            {
                minOrder = new Money(request.MinOrderAmount.Value, request.MinOrderCurrency!);
            }

            var created = Domain.CouponAggregate.Coupon.Create(
                rid,
                request.Code,
                request.Description,
                valueRes.Value,
                appliesRes.Value,
                request.ValidityStartDate,
                request.ValidityEndDate,
                minOrder,
                request.TotalUsageLimit,
                request.UsageLimitPerUser,
                request.IsEnabled);

            if (created.IsFailure)
            {
                return Result.Failure<CreateCouponResponse>(created.Error);
            }

            await _coupons.AddAsync(created.Value, cancellationToken);
            return Result.Success(new CreateCouponResponse(created.Value.Id.Value));
        }, cancellationToken);
    }
}

