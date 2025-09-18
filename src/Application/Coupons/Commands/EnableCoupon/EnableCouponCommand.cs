using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Coupons.Commands.EnableCoupon;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record EnableCouponCommand(
    Guid RestaurantId,
    Guid CouponId
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class EnableCouponCommandHandler : IRequestHandler<EnableCouponCommand, Result>
{
    private readonly ICouponRepository _coupons;
    private readonly IUnitOfWork _uow;

    public EnableCouponCommandHandler(ICouponRepository coupons, IUnitOfWork uow)
    {
        _coupons = coupons;
        _uow = uow;
    }

    public Task<Result> Handle(EnableCouponCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var cid = CouponId.Create(request.CouponId);
            var rid = RestaurantId.Create(request.RestaurantId);

            var coupon = await _coupons.GetByIdAsync(cid, cancellationToken);
            if (coupon is null)
            {
                return Result.Failure(Error.NotFound("Coupon.NotFound", $"Coupon with ID '{request.CouponId}' was not found."));
            }

            if (coupon.RestaurantId != rid)
            {
                throw new ForbiddenAccessException();
            }

            var res = coupon.Enable();
            if (res.IsFailure) return Result.Failure(res.Error);

            _coupons.Update(coupon);
            return Result.Success();
        }, cancellationToken);
    }
}

