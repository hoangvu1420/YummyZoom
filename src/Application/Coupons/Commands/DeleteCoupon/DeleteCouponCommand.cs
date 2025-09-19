using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Commands.DeleteCoupon;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record DeleteCouponCommand(
    Guid RestaurantId,
    Guid CouponId
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class DeleteCouponCommandValidator : AbstractValidator<DeleteCouponCommand>
{
    public DeleteCouponCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.CouponId).NotEmpty();
    }
}

public sealed class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Result>
{
    private readonly ICouponRepository _coupons;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public DeleteCouponCommandHandler(ICouponRepository coupons, IUnitOfWork uow, IUser user)
    {
        _coupons = coupons;
        _uow = uow;
        _user = user;
    }

    public Task<Result> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var rid = RestaurantId.Create(request.RestaurantId);
            var cid = CouponId.Create(request.CouponId);

            var coupon = await _coupons.GetByIdAsync(cid, cancellationToken);
            if (coupon is null)
            {
                return Result.Failure(DeleteCouponErrors.CouponNotFound(request.CouponId));
            }

            if (coupon.RestaurantId != rid)
            {
                throw new ForbiddenAccessException();
            }

            var deletedBy = _user.Id;
            var deleteResult = coupon.MarkAsDeleted(DateTimeOffset.UtcNow, deletedBy);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            _coupons.Update(coupon);

            return Result.Success();
        }, cancellationToken);
    }
}

public static class DeleteCouponErrors
{
    public static Error CouponNotFound(Guid couponId) => Error.NotFound(
        "Coupon.NotFound",
        $"Coupon with ID '{couponId}' was not found.");
}
