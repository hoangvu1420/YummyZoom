using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Tests for edge cases and boundary conditions of OrderFinancialService.ValidateAndCalculateDiscount method.
/// </summary>
public class ValidateAndCalculateDiscountEdgeCaseTests : OrderFinancialServiceTestsBase
{
    #region Boundary Value Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithCouponValidAtExactStartTime_ReturnsDiscount()
    {
        // Arrange
        var exactStartTime = _fixedDateTime;
        var coupon = CreateValidCoupon(
            validityStartDate: exactStartTime,
            validityEndDate: exactStartTime.AddDays(30));
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var expectedDiscount = new Money(2.00m, "USD"); // 10% default

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, exactStartTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "coupon should be valid at exact start time");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithCouponValidAtExactEndTime_ReturnsDiscount()
    {
        // Arrange
        var exactEndTime = _fixedDateTime;
        var coupon = CreateValidCoupon(
            validityStartDate: exactEndTime.AddDays(-30),
            validityEndDate: exactEndTime);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var expectedDiscount = new Money(2.00m, "USD"); // 10% default

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, exactEndTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "coupon should be valid at exact end time");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithUsageLimitAtBoundary_ReturnsDiscount()
    {
        // Arrange
        var coupon = CreateValidCoupon(
            totalUsageLimit: 10,
            currentTotalUsageCount: 9); // One below limit
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var expectedDiscount = new Money(2.00m, "USD"); // 10% default

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should work when usage is one below limit");
    }

    #endregion

    #region Precision and Rounding Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithVerySmallPercentage_ReturnsMinimalDiscount()
    {
        // Arrange
        var coupon = CreatePercentageCoupon(0.01m); // 0.01% discount
        var orderItems = CreateOrderItems((1000.00m, 1, null, null));
        var subtotal = new Money(1000.00m, "USD");
        var expectedDiscount = new Money(0.10m, "USD"); // 0.01% of 1000.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should handle very small percentage discounts");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithComplexDecimalCalculation_MaintainsPrecision()
    {
        // Arrange
        var coupon = CreatePercentageCoupon(33.33m); // 33.33% discount
        var orderItems = CreateOrderItems((9.99m, 1, null, null));
        var subtotal = new Money(9.99m, "USD");
        var expectedDiscount = new Money(3.3297m, "USD"); // 33.33% of 9.99 = 3.329667...

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        // Allow for small rounding differences
        result.ValueOrFail().Amount.Should().BeApproximately(expectedDiscount.Amount, 0.01m, 
            "should maintain reasonable precision in decimal calculations");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithVeryLargeAmounts_HandlesCorrectly()
    {
        // Arrange
        var coupon = CreatePercentageCoupon(5m); // 5% discount
        var orderItems = CreateOrderItems((999999.99m, 1, null, null));
        var subtotal = new Money(999999.99m, "USD");
        var expectedDiscount = new Money(49999.9995m, "USD"); // 5% of 999999.99

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        result.ValueOrFail().Amount.Should().BeApproximately(expectedDiscount.Amount, 0.01m,
            "should handle very large amounts correctly");
    }

    #endregion

    #region Currency Edge Cases

    [Test]
    public void ValidateAndCalculateDiscount_WithDifferentCurrencies_UsesSubtotalCurrency()
    {
        // Arrange
        var coupon = CreateFixedAmountCoupon(5.00m, "EUR"); // Coupon in EUR
        var orderItems = CreateOrderItems((20.00m, 1, null, null)); // Order in USD
        var subtotal = new Money(20.00m, "USD"); // Subtotal in USD
        var expectedDiscount = new Money(5.00m, "USD"); // Should use USD from subtotal

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        result.ValueOrFail().Currency.Should().Be("USD", "should use subtotal currency for discount");
        result.ValueOrFail().Amount.Should().Be(5.00m, "should use coupon amount value");
    }

    #endregion

    #region Multiple Items Edge Cases

    [Test]
    public void ValidateAndCalculateDiscount_WithMixedApplicableAndNonApplicableItems_CalculatesCorrectly()
    {
        // Arrange
        var applicableItemId = MenuItemId.CreateUnique();
        var nonApplicableItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(10.00m, 2, menuItemId: applicableItemId),     // 20.00 applicable
            CreateOrderItem(15.00m, 1, menuItemId: nonApplicableItemId),  // 15.00 not applicable
            CreateOrderItem(5.00m, 1, menuItemId: applicableItemId)       // 5.00 applicable
        };
        var coupon = CreatePercentageCoupon(20m, CouponScope.SpecificItems, 
            new List<MenuItemId> { applicableItemId });
        var subtotal = new Money(40.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD"); // 20% of (20.00 + 5.00)

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), 
            "should only apply discount to applicable items");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithMultipleCategoriesInOrder_CalculatesCorrectly()
    {
        // Arrange
        var applicableCategoryId = MenuCategoryId.CreateUnique();
        var nonApplicableCategoryId = MenuCategoryId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(12.00m, 1, categoryId: applicableCategoryId),     // 12.00 applicable
            CreateOrderItem(8.00m, 2, categoryId: nonApplicableCategoryId),   // 16.00 not applicable
            CreateOrderItem(6.00m, 1, categoryId: applicableCategoryId)       // 6.00 applicable
        };
        var coupon = CreatePercentageCoupon(25m, CouponScope.SpecificCategories,
            categoryIds: new List<MenuCategoryId> { applicableCategoryId });
        var subtotal = new Money(34.00m, "USD");
        var expectedDiscount = new Money(4.50m, "USD"); // 25% of (12.00 + 6.00)

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(),
            "should only apply discount to items in applicable categories");
    }

    #endregion

    #region Free Item Edge Cases

    [Test]
    public void ValidateAndCalculateDiscount_WithFreeItemHavingZeroPrice_ReturnsZeroDiscount()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(0.00m, 1, menuItemId: freeItemId), // Free item with zero price
            CreateOrderItem(10.00m, 1) // Regular item
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(10.00m, "USD");
        var expectedDiscount = Money.Zero("USD"); // Zero because item is already free

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(),
            "free item coupon on zero-price item should return zero discount");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithFreeItemCouponAndMultipleInstancesOfDifferentPrices_ReturnsLowestUnitPrice()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(15.00m, 2, menuItemId: freeItemId), // Unit price: 15.00, Total: 30.00
            CreateOrderItem(5.00m, 4, menuItemId: freeItemId),  // Unit price: 5.00 (lowest), Total: 20.00
            CreateOrderItem(8.00m, 3, menuItemId: freeItemId)   // Unit price: 8.00, Total: 24.00
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(74.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD"); // Lowest unit price

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(),
            "should return the lowest unit price among multiple instances");
    }

    #endregion

    #region Discount Capping Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithFixedAmountExceedingApplicableAmount_CapsAtApplicableAmount()
    {
        // Arrange
        var applicableItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(5.00m, 1, menuItemId: applicableItemId),  // Only 5.00 applicable
            CreateOrderItem(20.00m, 1) // Not applicable
        };
        var coupon = CreateValidCoupon(
            couponValue: CouponValue.CreateFixedAmount(new Money(10.00m, "USD")).Value, // Discount > applicable amount
            appliesTo: AppliesTo.CreateForSpecificItems(new List<MenuItemId> { applicableItemId }).Value);
        var subtotal = new Money(25.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD"); // Capped at applicable amount

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(),
            "fixed amount discount should be capped at applicable amount");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithPercentageResultingInExcessiveDiscount_CapsAtDiscountBase()
    {
        // Arrange
        var applicableItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(10.00m, 1, menuItemId: applicableItemId), // Only 10.00 applicable
            CreateOrderItem(50.00m, 1) // Not applicable
        };
        var coupon = CreatePercentageCoupon(100m, CouponScope.SpecificItems, 
            new List<MenuItemId> { applicableItemId }); // 100% discount
        var subtotal = new Money(60.00m, "USD");
        var expectedDiscount = new Money(10.00m, "USD"); // Capped at applicable amount

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(),
            "percentage discount should be capped at discount base amount");
    }

    #endregion

    #region Time Zone and DateTime Edge Cases

    [Test]
    public void ValidateAndCalculateDiscount_WithMillisecondPrecisionAtBoundary_HandlesCorrectly()
    {
        // Arrange
        var preciseEndTime = new DateTime(2024, 1, 15, 23, 59, 59, 999, DateTimeKind.Utc);
        var coupon = CreateValidCoupon(
            validityStartDate: preciseEndTime.AddDays(-30),
            validityEndDate: preciseEndTime);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var checkTime = preciseEndTime.AddMilliseconds(1); // Just after expiry

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, orderItems, subtotal, checkTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponExpired.Code);
    }

    #endregion
}
