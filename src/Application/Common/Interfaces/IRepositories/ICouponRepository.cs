using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByIdAsync(CouponId couponId, CancellationToken cancellationToken = default);
    Task<int> GetUserUsageCountAsync(CouponId couponId, UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    void Update(Coupon coupon);
}
