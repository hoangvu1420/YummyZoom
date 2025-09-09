using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

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

    public async Task<bool> TryIncrementUsageCountAsync(CouponId couponId, CancellationToken cancellationToken = default)
    {
        // Perform atomic increment with condition check to prevent race conditions
        var sql = """
            UPDATE "Coupons" 
            SET "CurrentTotalUsageCount" = "CurrentTotalUsageCount" + 1 
            WHERE "Id" = {0} 
              AND ("TotalUsageLimit" IS NULL OR "CurrentTotalUsageCount" < "TotalUsageLimit")
            """;

        try
        {
            var rowsAffected = await _dbContext.Database.ExecuteSqlRawAsync(sql, [couponId.Value], cancellationToken);
            return rowsAffected == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> TryIncrementUserUsageCountAsync(
        CouponId couponId,
        UserId userId,
        int? perUserLimit,
        CancellationToken cancellationToken = default)
    {
        if (perUserLimit.HasValue)
        {
            var sql = """
                INSERT INTO "CouponUserUsages" ("CouponId", "UserId", "UsageCount")
                VALUES ({0}, {1}, 1)
                ON CONFLICT ("CouponId", "UserId")
                DO UPDATE SET "UsageCount" = "CouponUserUsages"."UsageCount" + 1
                WHERE "CouponUserUsages"."UsageCount" < {2};
                """;

            var rows = await _dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [couponId.Value, userId.Value, perUserLimit.Value],
                cancellationToken);
            return rows == 1;
        }
        else
        {
            var sql = """
                INSERT INTO "CouponUserUsages" ("CouponId", "UserId", "UsageCount")
                VALUES ({0}, {1}, 1)
                ON CONFLICT ("CouponId", "UserId")
                DO UPDATE SET "UsageCount" = "CouponUserUsages"."UsageCount" + 1;
                """;

            var rows = await _dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [couponId.Value, userId.Value],
                cancellationToken);
            return rows == 1;
        }
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
