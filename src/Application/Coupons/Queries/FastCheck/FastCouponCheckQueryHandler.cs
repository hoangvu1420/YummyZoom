using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed class FastCouponCheckQueryHandler : IRequestHandler<FastCouponCheckQuery, Result<FastCouponCheckResponse>>
{
    private readonly ICouponRepository _couponRepository;
    private readonly OrderFinancialService _financials;
    private readonly IUser _currentUser;
    private readonly ILogger<FastCouponCheckQueryHandler> _logger;

    public FastCouponCheckQueryHandler(
        ICouponRepository couponRepository,
        OrderFinancialService financials,
        IUser currentUser,
        ILogger<FastCouponCheckQueryHandler> logger)
    {
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
        _financials = financials ?? throw new ArgumentNullException(nameof(financials));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<FastCouponCheckResponse>> Handle(FastCouponCheckQuery request, CancellationToken ct)
    {
        if (_currentUser.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var restaurantId = RestaurantId.Create(request.RestaurantId);
        var userId = _currentUser.DomainUserId;

        // 1) Build temporary OrderItems from request snapshot (parity with order math)
        var currency = Currencies.Default; // phase 1: assume default currency for calculation
        var orderItems = new List<OrderItem>(capacity: request.Items.Count);
        foreach (var it in request.Items)
        {
            if (it.Qty <= 0) continue;
            var createItem = OrderItem.Create(
                MenuCategoryId.Create(it.MenuCategoryId),
                MenuItemId.Create(it.MenuItemId),
                snapshotItemName: "N/A",
                snapshotBasePriceAtOrder: new Money(it.UnitPrice, currency),
                quantity: it.Qty,
                selectedCustomizations: null);

            if (createItem.IsFailure) continue; // skip malformed entries
            orderItems.Add(createItem.Value);
        }

        var subtotal = _financials.CalculateSubtotal(orderItems);

        // 2) Fetch candidate coupons (active now)
        var now = DateTime.UtcNow;
        var candidates = await _couponRepository.GetActiveByRestaurantAsync(restaurantId, now, ct);

        var resultList = new List<FastCouponCandidateDto>(candidates.Count);

        foreach (var coupon in candidates)
        {
            // Usage limits quick checks (phase 1): total + per-user
            if (coupon.TotalUsageLimit.HasValue && coupon.CurrentTotalUsageCount >= coupon.TotalUsageLimit.Value)
            {
                resultList.Add(ToDto(coupon, 0m, meetsMin: true, minGap: 0m, reason: "UsageLimitExceeded"));
                continue;
            }

            if (coupon.UsageLimitPerUser.HasValue)
            {
                var usedByUser = await _couponRepository.GetUserUsageCountAsync(coupon.Id, userId, ct);
                if (usedByUser >= coupon.UsageLimitPerUser.Value)
                {
                    resultList.Add(ToDto(coupon, 0m, meetsMin: true, minGap: 0m, reason: "UserUsageLimitExceeded"));
                    continue;
                }
            }

            // 3) Validate + compute discount using canonical service
            var calc = _financials.ValidateAndCalculateDiscount(coupon, orderItems, subtotal, now);
            if (calc.IsFailure)
            {
                var reason = calc.Error.Code; // surface precise reason where possible
                decimal minGap = 0m;
                var meets = true;
                if (calc.Error.Code.EndsWith("MinAmountNotMet", StringComparison.OrdinalIgnoreCase) && coupon.MinOrderAmount is not null)
                {
                    var gap = Math.Max(0m, coupon.MinOrderAmount.Amount - subtotal.Amount);
                    minGap = Math.Round(gap, 2);
                    meets = false;
                }
                resultList.Add(ToDto(coupon, 0m, meets, minGap, reason));
                continue;
            }

            var savings = Math.Round(calc.Value.Amount, 2);
            resultList.Add(ToDto(coupon, savings, true, 0m, null));
        }

        // 4) Rank: savings DESC, expires sooner, scope simplicity
        var ranked = resultList
            .OrderByDescending(c => c.Savings)
            .ThenBy(c => c.ValidityEnd)
            .ThenBy(c => ScopeRank(c.Scope))
            .ToList();

        var best = ranked.FirstOrDefault(c => c.Savings > 0);
        return Result.Success(new FastCouponCheckResponse(best, ranked));
    }

    private static int ScopeRank(string scope) => scope switch
    {
        nameof(CouponScope.WholeOrder) => 0,
        nameof(CouponScope.SpecificCategories) => 1,
        nameof(CouponScope.SpecificItems) => 2,
        _ => 3
    };

    private static FastCouponCandidateDto ToDto(Coupon coupon, decimal savings, bool meetsMin, decimal minGap, string? reason)
    {
        var label = coupon.Value.Type switch
        {
            CouponType.Percentage => $"{coupon.Value.PercentageValue}% off",
            CouponType.FixedAmount => $"{coupon.Value.FixedAmountValue?.Amount} off",
            CouponType.FreeItem => "Free item",
            _ => "Coupon"
        };

        return new FastCouponCandidateDto(
            Code: coupon.Code,
            Label: label,
            Savings: savings,
            MeetsMinOrder: meetsMin,
            MinOrderGap: minGap,
            ValidityEnd: coupon.ValidityEndDate,
            Scope: coupon.AppliesTo.Scope.ToString(),
            ReasonIfIneligible: reason
        );
    }
}
