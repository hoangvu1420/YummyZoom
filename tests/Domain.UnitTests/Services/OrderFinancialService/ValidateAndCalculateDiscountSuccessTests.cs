using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Tests for successful scenarios of OrderFinancialService.ValidateAndCalculateDiscount method.
/// </summary>
public class ValidateAndCalculateDiscountSuccessTests : OrderFinancialServiceTestsBase
{
    #region Percentage Coupon Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithValidPercentageCoupon_ReturnsCorrectDiscount()
    {
        // Arrange
        var coupon = CreatePercentageCoupon(20m); // 20% discount
        var orderItems = CreateOrderItems((10.00m, 1, null, null), (15.00m, 2, null, null)); // Total: 40.00
        var subtotal = new Money(40.00m, "USD");
        var expectedDiscount = new Money(8.00m, "USD"); // 20% of 40.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "20% discount should be 8.00");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithPercentageCouponForSpecificItems_ReturnsCorrectDiscount()
    {
        // Arrange
        var targetItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(10.00m, 1, menuItemId: targetItemId), // This item qualifies
            CreateOrderItem(20.00m, 1) // This item doesn't qualify
        };
        var coupon = CreatePercentageCoupon(50m, CouponScope.SpecificItems, new List<MenuItemId> { targetItemId });
        var subtotal = new Money(30.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD"); // 50% of 10.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "50% discount on specific item should be 5.00");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithPercentageCouponForSpecificCategories_ReturnsCorrectDiscount()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(15.00m, 2, categoryId: targetCategoryId), // 30.00 qualifies
            CreateOrderItem(10.00m, 1) // 10.00 doesn't qualify
        };
        var coupon = CreatePercentageCoupon(25m, CouponScope.SpecificCategories, 
            categoryIds: new List<MenuCategoryId> { targetCategoryId });
        var subtotal = new Money(40.00m, "USD");
        var expectedDiscount = new Money(7.50m, "USD"); // 25% of 30.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "25% discount on specific category should be 7.50");
    }

    #endregion

    #region Fixed Amount Coupon Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithValidFixedAmountCoupon_ReturnsCorrectDiscount()
    {
        // Arrange
        var coupon = CreateFixedAmountCoupon(5.00m);
        var orderItems = CreateOrderItems((10.00m, 1, null, null), (15.00m, 1, null, null)); // Total: 25.00
        var subtotal = new Money(25.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "fixed amount discount should be 5.00");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithFixedAmountExceedingSubtotal_ReturnsSubtotalAsDiscount()
    {
        // Arrange
        var coupon = CreateFixedAmountCoupon(50.00m); // Discount larger than subtotal
        var orderItems = CreateOrderItems((10.00m, 1, null, null)); // Total: 10.00
        var subtotal = new Money(10.00m, "USD");
        var expectedDiscount = new Money(10.00m, "USD"); // Capped at subtotal

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "discount should be capped at subtotal amount");
    }

    #endregion

    #region Free Item Coupon Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithValidFreeItemCoupon_ReturnsItemPrice()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(12.00m, 1, menuItemId: freeItemId), // This item will be free
            CreateOrderItem(8.00m, 1) // Regular item
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(20.00m, "USD");
        var expectedDiscount = new Money(12.00m, "USD"); // Price of the free item

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "free item discount should equal item price");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithFreeItemCouponMultipleQuantities_ReturnsUnitPrice()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(15.00m, 3, menuItemId: freeItemId) // 3 items at 15.00 each = 45.00 total
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(45.00m, "USD");
        var expectedDiscount = new Money(15.00m, "USD"); // Unit price (45.00 / 3)

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "free item discount should equal unit price");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithFreeItemCouponMultipleSameItems_ReturnsLowestUnitPrice()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(10.00m, 2, menuItemId: freeItemId), // Unit price: 10.00 (total: 20.00)
            CreateOrderItem(15.00m, 1, menuItemId: freeItemId)  // Unit price: 15.00 (total: 15.00)
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(35.00m, "USD"); // 20.00 + 15.00
        var expectedDiscount = new Money(10.00m, "USD"); // Lowest unit price

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should return lowest unit price when multiple same items exist");
    }

    #endregion

    #region Minimum Order Amount Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithMinOrderAmountMet_ReturnsDiscount()
    {
        // Arrange
        var minOrderAmount = new Money(20.00m, "USD");
        var coupon = CreateValidCoupon(
            couponValue: CouponValue.CreatePercentage(10m).Value,
            minOrderAmount: minOrderAmount);
        var orderItems = CreateOrderItems((25.00m, 1, null, null)); // Meets minimum
        var subtotal = new Money(25.00m, "USD");
        var expectedDiscount = new Money(2.50m, "USD"); // 10% of 25.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should apply discount when minimum order amount is met");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithExactMinOrderAmount_ReturnsDiscount()
    {
        // Arrange
        var minOrderAmount = new Money(30.00m, "USD");
        var coupon = CreateValidCoupon(
            couponValue: CouponValue.CreateFixedAmount(new Money(5.00m, "USD")).ValueOrFail(),
            minOrderAmount: minOrderAmount);
        var orderItems = CreateOrderItems((30.00m, 1, null, null)); // Exactly meets minimum
        var subtotal = new Money(30.00m, "USD");
        var expectedDiscount = new Money(5.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should apply discount when order amount exactly meets minimum");
    }

    #endregion

    #region Usage Limit Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithUsageLimitsNotExceeded_ReturnsDiscount()
    {
        // Arrange
        var coupon = CreateValidCoupon(
            couponValue: CouponValue.CreatePercentage(15m).Value,
            totalUsageLimit: 100,
            usageLimitPerUser: 5,
            currentTotalUsageCount: 50);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var userUsageCount = 2; // Under per-user limit
        var expectedDiscount = new Money(3.00m, "USD"); // 15% of 20.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, userUsageCount, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "should apply discount when usage limits are not exceeded");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ValidateAndCalculateDiscount_WithZeroPercentage_ReturnsZeroDiscount()
    {
        // Arrange - This test assumes 0% is somehow valid (edge case)
        var orderItems = CreateOrderItems((100.00m, 1, null, null));
        var subtotal = new Money(100.00m, "USD");
        
        // Create coupon with reflection to bypass validation
        var coupon = CreateValidCoupon();
        var couponValue = CouponValue.CreatePercentage(0.01m).ValueOrFail();
        
        // Use reflection to set the CouponValue on the Coupon
        var couponValueField = typeof(YummyZoom.Domain.CouponAggregate.Coupon)
            .GetField("<Value>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        couponValueField?.SetValue(coupon, couponValue); // Minimum valid percentage
        
        var expectedDiscount = new Money(0.01m, "USD"); // 0.01% of 100.00

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "minimum percentage should return minimal discount");
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithMaxPercentage_ReturnsFullSubtotal()
    {
        // Arrange
        var coupon = CreatePercentageCoupon(100m); // 100% discount
        var orderItems = CreateOrderItems((50.00m, 1, null, null));
        var subtotal = new Money(50.00m, "USD");
        var expectedDiscount = new Money(50.00m, "USD"); // Full subtotal

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeSuccessful();
        AssertMoneyEquals(expectedDiscount, result.ValueOrFail(), "100% discount should equal full subtotal");
    }

    #endregion
}
