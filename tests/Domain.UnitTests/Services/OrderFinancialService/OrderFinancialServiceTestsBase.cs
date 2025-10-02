using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Base class for OrderFinancialService unit tests providing common setup and helper methods.
/// </summary>
public abstract class OrderFinancialServiceTestsBase
{
    protected readonly Domain.Services.OrderFinancialService _orderFinancialService;
    protected readonly DateTime _fixedDateTime = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    protected readonly string _defaultCurrency = "USD";

    protected OrderFinancialServiceTestsBase()
    {
        _orderFinancialService = new Domain.Services.OrderFinancialService();
    }

    #region Order Item Builders

    protected OrderItem CreateOrderItem(
        decimal basePriceAmount = 10.00m,
        int quantity = 1,
        string currency = "USD",
        MenuItemId? menuItemId = null,
        MenuCategoryId? categoryId = null,
        string itemName = "Test Item")
    {
        var basePrice = new Money(basePriceAmount, currency);
        var menuItem = menuItemId ?? MenuItemId.CreateUnique();
        var category = categoryId ?? MenuCategoryId.CreateUnique();

        return OrderItem.Create(
            category,
            menuItem,
            itemName,
            basePrice,
            quantity).ValueOrFail();
    }

    protected List<OrderItem> CreateOrderItems(params (decimal price, int quantity, MenuItemId? itemId, MenuCategoryId? categoryId)[] items)
    {
        var orderItems = new List<OrderItem>();

        foreach (var (price, quantity, itemId, categoryId) in items)
        {
            orderItems.Add(CreateOrderItem(
                basePriceAmount: price,
                quantity: quantity,
                menuItemId: itemId,
                categoryId: categoryId));
        }

        return orderItems;
    }

    #endregion

    #region Coupon Builders

    protected Coupon CreateValidCoupon(
        CouponValue? couponValue = null,
        AppliesTo? appliesTo = null,
        bool isEnabled = true,
        DateTime? validityStartDate = null,
        DateTime? validityEndDate = null,
        Money? minOrderAmount = null,
        int? totalUsageLimit = null,
        int? usageLimitPerUser = null,
        int currentTotalUsageCount = 0,
        RestaurantId? restaurantId = null)
    {
        var restaurant = restaurantId ?? RestaurantId.CreateUnique();
        var value = couponValue ?? CouponValue.CreatePercentage(10m).ValueOrFail();
        var scope = appliesTo ?? AppliesTo.CreateForWholeOrder().ValueOrFail();
        var startDate = validityStartDate ?? _fixedDateTime.AddDays(-1);
        var endDate = validityEndDate ?? _fixedDateTime.AddDays(30);

        Coupon coupon;

        if (currentTotalUsageCount > 0)
        {
            // Use persistence overload when we need to set current usage count
            coupon = Coupon.Create(
                CouponId.CreateUnique(),
                restaurant,
                "TESTCODE",
                "Test Coupon Description",
                value,
                scope,
                startDate,
                endDate,
                currentTotalUsageCount,
                minOrderAmount,
                totalUsageLimit,
                usageLimitPerUser,
                isEnabled).ValueOrFail();
        }
        else
        {
            // Use regular creation overload
            coupon = Coupon.Create(
                restaurant,
                "TESTCODE",
                "Test Coupon Description",
                value,
                scope,
                startDate,
                endDate,
                minOrderAmount,
                totalUsageLimit,
                usageLimitPerUser,
                isEnabled).ValueOrFail();
        }

        return coupon;
    }

    protected Coupon CreatePercentageCoupon(
        decimal percentage,
        CouponScope scope = CouponScope.WholeOrder,
        List<MenuItemId>? itemIds = null,
        List<MenuCategoryId>? categoryIds = null)
    {
        var couponValue = CouponValue.CreatePercentage(percentage).ValueOrFail();
        var appliesTo = scope switch
        {
            CouponScope.WholeOrder => AppliesTo.CreateForWholeOrder().ValueOrFail(),
            CouponScope.SpecificItems => AppliesTo.CreateForSpecificItems(itemIds ?? new List<MenuItemId>()).ValueOrFail(),
            CouponScope.SpecificCategories => AppliesTo.CreateForSpecificCategories(categoryIds ?? new List<MenuCategoryId>()).ValueOrFail(),
            _ => AppliesTo.CreateForWholeOrder().ValueOrFail()
        };

        return CreateValidCoupon(couponValue: couponValue, appliesTo: appliesTo);
    }

    protected Coupon CreateFixedAmountCoupon(
        decimal amount,
        string currency = "USD",
        CouponScope scope = CouponScope.WholeOrder)
    {
        var couponValue = CouponValue.CreateFixedAmount(new Money(amount, currency)).ValueOrFail();
        var appliesTo = AppliesTo.CreateForWholeOrder().ValueOrFail(); // Simplified for base class

        return CreateValidCoupon(couponValue: couponValue, appliesTo: appliesTo);
    }

    protected Coupon CreateFreeItemCoupon(
        MenuItemId freeItemId,
        CouponScope scope = CouponScope.WholeOrder)
    {
        var couponValue = CouponValue.CreateFreeItem(freeItemId).ValueOrFail();
        var appliesTo = AppliesTo.CreateForWholeOrder().ValueOrFail();

        return CreateValidCoupon(couponValue: couponValue, appliesTo: appliesTo);
    }

    #endregion

    #region Money Helpers

    protected Money CreateMoney(decimal amount, string currency = "USD")
    {
        return new Money(amount, currency);
    }

    protected Money ZeroMoney(string currency = "USD")
    {
        return Money.Zero(currency);
    }

    #endregion

    #region Assertion Helpers

    protected void AssertMoneyEquals(Money expected, Money actual, string? because = null)
    {
        actual.Amount.Should().Be(expected.Amount, because);
        actual.Currency.Should().Be(expected.Currency, because);
    }

    protected void AssertMoneyIsZero(Money money, string? because = null)
    {
        money.Amount.Should().Be(0m, because);
    }

    protected void AssertMoneyIsPositive(Money money, string? because = null)
    {
        money.Amount.Should().BeGreaterThan(0m, because);
    }

    #endregion
}
