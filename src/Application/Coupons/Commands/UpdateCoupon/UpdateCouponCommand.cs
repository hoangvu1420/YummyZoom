using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Commands.UpdateCoupon;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateCouponCommand(
    Guid RestaurantId,
    Guid CouponId,
    string Description,
    DateTime ValidityStartDate,
    DateTime ValidityEndDate,
    CouponType ValueType,
    decimal? Percentage,
    decimal? FixedAmount,
    string? FixedCurrency,
    Guid? FreeItemId,
    CouponScope Scope,
    List<Guid>? ItemIds,
    List<Guid>? CategoryIds,
    decimal? MinOrderAmount,
    string? MinOrderCurrency,
    int? TotalUsageLimit,
    int? UsageLimitPerUser
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.CouponId).NotEmpty();
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

public sealed class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result>
{
    private readonly ICouponRepository _coupons;
    private readonly IMenuItemRepository _menuItems;
    private readonly IMenuCategoryRepository _menuCategories;
    private readonly IMenuRepository _menus;
    private readonly IUnitOfWork _uow;

    public UpdateCouponCommandHandler(
        ICouponRepository coupons,
        IMenuItemRepository menuItems,
        IMenuCategoryRepository menuCategories,
        IMenuRepository menus,
        IUnitOfWork uow)
    {
        _coupons = coupons;
        _menuItems = menuItems;
        _menuCategories = menuCategories;
        _menus = menus;
        _uow = uow;
    }

    public Task<Result> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var rid = RestaurantId.Create(request.RestaurantId);
            var cid = CouponId.Create(request.CouponId);

            var coupon = await _coupons.GetByIdAsync(cid, cancellationToken);
            if (coupon is null)
            {
                return Result.Failure(UpdateCouponErrors.CouponNotFound(request.CouponId));
            }

            // Ensure scoping to the restaurant
            if (coupon.RestaurantId != rid)
            {
                throw new ForbiddenAccessException();
            }

            // Build new value
            Result<CouponValue> valueRes = request.ValueType switch
            {
                CouponType.Percentage => CouponValue.CreatePercentage(request.Percentage!.Value),
                CouponType.FixedAmount => CouponValue.CreateFixedAmount(new Money(request.FixedAmount!.Value, request.FixedCurrency!)),
                CouponType.FreeItem => CouponValue.CreateFreeItem(MenuItemId.Create(request.FreeItemId!.Value)),
                _ => Result.Failure<CouponValue>(Error.Validation("Coupon.ValueTypeInvalid", "Unsupported coupon value type."))
            };
            if (valueRes.IsFailure) return Result.Failure(valueRes.Error);

            // Validate applies-to IDs belong to the same restaurant
            Result<AppliesTo> appliesRes;
            switch (request.Scope)
            {
                case CouponScope.WholeOrder:
                    appliesRes = AppliesTo.CreateForWholeOrder();
                    break;
                case CouponScope.SpecificItems:
                    {
                        if (request.ItemIds is null || request.ItemIds.Count == 0)
                            return Result.Failure(Error.Validation("Coupon.ItemIdsRequired", "At least one ItemId is required when Scope is SpecificItems."));

                        // Validate each item belongs to the restaurant
                        foreach (var gid in request.ItemIds)
                        {
                            var itemRid = await _menuItems.GetRestaurantIdByIdIncludingDeletedAsync(MenuItemId.Create(gid), cancellationToken);
                            if (itemRid is null || itemRid.Value != request.RestaurantId)
                            {
                                return Result.Failure(Error.Validation("Coupon.InvalidItemRestaurant", $"Menu item {gid} does not belong to restaurant {request.RestaurantId}."));
                            }
                        }

                        appliesRes = AppliesTo.CreateForSpecificItems(request.ItemIds.Select(MenuItemId.Create).ToList());
                        break;
                    }
                case CouponScope.SpecificCategories:
                    {
                        if (request.CategoryIds is null || request.CategoryIds.Count == 0)
                            return Result.Failure(Error.Validation("Coupon.CategoryIdsRequired", "At least one CategoryId is required when Scope is SpecificCategories."));

                        foreach (var gid in request.CategoryIds)
                        {
                            var category = await _menuCategories.GetByIdAsync(MenuCategoryId.Create(gid), cancellationToken);
                            if (category is null)
                            {
                                return Result.Failure(Error.Validation("Coupon.CategoryNotFound", $"Menu category {gid} was not found."));
                            }

                            var menu = await _menus.GetByIdAsync(category.MenuId, cancellationToken);
                            if (menu is null || menu.RestaurantId != rid)
                            {
                                return Result.Failure(Error.Validation("Coupon.InvalidCategoryRestaurant", $"Menu category {gid} does not belong to restaurant {request.RestaurantId}."));
                            }
                        }

                        appliesRes = AppliesTo.CreateForSpecificCategories(request.CategoryIds.Select(MenuCategoryId.Create).ToList());
                        break;
                    }
                default:
                    appliesRes = Result.Failure<AppliesTo>(Error.Validation("Coupon.ScopeInvalid", "Unsupported coupon scope."));
                    break;
            }

            if (appliesRes.IsFailure) return Result.Failure(appliesRes.Error);

            // Apply updates
            var descRes = coupon.UpdateDescription(request.Description);
            if (descRes.IsFailure) return Result.Failure(descRes.Error);

            var datesRes = coupon.SetValidityPeriod(request.ValidityStartDate, request.ValidityEndDate);
            if (datesRes.IsFailure) return Result.Failure(datesRes.Error);

            var setValRes = coupon.SetValue(valueRes.Value);
            if (setValRes.IsFailure) return Result.Failure(setValRes.Error);

            var setAppliesRes = coupon.SetAppliesTo(appliesRes.Value);
            if (setAppliesRes.IsFailure) return Result.Failure(setAppliesRes.Error);

            if (request.MinOrderAmount.HasValue)
            {
                var minOrderRes = coupon.SetMinimumOrderAmount(new Money(request.MinOrderAmount.Value, request.MinOrderCurrency!));
                if (minOrderRes.IsFailure) return Result.Failure(minOrderRes.Error);
            }
            else
            {
                // Explicitly remove when null to support clearing
                var removeMinRes = coupon.RemoveMinimumOrderAmount();
                if (removeMinRes.IsFailure) return Result.Failure(removeMinRes.Error);
            }

            // Optional usage limits
            if (request.TotalUsageLimit.HasValue)
            {
                var res = coupon.SetTotalUsageLimit(request.TotalUsageLimit);
                if (res.IsFailure) return Result.Failure(res.Error);
            }

            if (request.UsageLimitPerUser.HasValue)
            {
                var res = coupon.SetPerUserUsageLimit(request.UsageLimitPerUser);
                if (res.IsFailure) return Result.Failure(res.Error);
            }

            _coupons.Update(coupon);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class UpdateCouponErrors
{
    public static Error CouponNotFound(Guid couponId) => Error.NotFound(
        "Coupon.NotFound",
        $"Coupon with ID '{couponId}' was not found.");
}
