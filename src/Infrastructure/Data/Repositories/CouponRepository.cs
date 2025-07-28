using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CouponRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Coupon?> GetByCodeAsync(string code, RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Code == code && c.RestaurantId == restaurantId, cancellationToken);
    }

    public async Task<Coupon?> GetByIdAsync(CouponId couponId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Coupons
            .FirstOrDefaultAsync(c => c.Id == couponId, cancellationToken);
    }

    public async Task<int> GetUserUsageCountAsync(CouponId couponId, UserId userId, CancellationToken cancellationToken = default)
    {
        // Count how many times this user has used this coupon
        // This assumes there's a relationship between Order and Coupon through AppliedCouponID
        // TODO: Enhance further.
        return await _dbContext.Orders
            .Where(o => o.AppliedCouponId == couponId && o.CustomerId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await _dbContext.Coupons.AddAsync(coupon, cancellationToken);
    }

    public void Update(Coupon coupon)
    {
        _dbContext.Coupons.Update(coupon);
    }
}
