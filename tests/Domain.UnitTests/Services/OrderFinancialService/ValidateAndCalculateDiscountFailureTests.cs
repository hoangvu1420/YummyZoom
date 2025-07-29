using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Tests for failure scenarios of OrderFinancialService.ValidateAndCalculateDiscount method.
/// </summary>
public class ValidateAndCalculateDiscountFailureTests : OrderFinancialServiceTestsBase
{
    #region Coupon Status Validation Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithDisabledCoupon_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: false);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponDisabled.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithExpiredCoupon_ReturnsFailure()
    {
        // Arrange
        var expiredEndDate = _fixedDateTime.AddDays(-1); // Expired yesterday
        var validStartDate = _fixedDateTime.AddDays(-30); // Started 30 days ago
        var coupon = CreateValidCoupon(
            validityStartDate: validStartDate,
            validityEndDate: expiredEndDate);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponExpired.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithNotYetValidCoupon_ReturnsFailure()
    {
        // Arrange
        var futureStartDate = _fixedDateTime.AddDays(1); // Starts tomorrow
        var futureEndDate = _fixedDateTime.AddDays(30);
        var coupon = CreateValidCoupon(
            validityStartDate: futureStartDate,
            validityEndDate: futureEndDate);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponNotYetValid.Code);
    }

    #endregion

    #region Usage Limit Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithTotalUsageLimitExceeded_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon(
            totalUsageLimit: 10,
            currentTotalUsageCount: 10); // At limit
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.UsageLimitExceeded.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithTotalUsageLimitExceededByOne_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon(
            totalUsageLimit: 5,
            currentTotalUsageCount: 5); // At limit, so next use will exceed
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.UsageLimitExceeded.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithUserUsageLimitExceeded_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon(usageLimitPerUser: 3);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var userUsageCount = 3; // At limit

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, userUsageCount, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.UserUsageLimitExceeded.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithUserUsageLimitExceededByOne_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon(usageLimitPerUser: 2);
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        var userUsageCount = 3; // Over limit

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, userUsageCount, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.UserUsageLimitExceeded.Code);
    }

    #endregion

    #region Minimum Order Amount Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithMinOrderAmountNotMet_ReturnsFailure()
    {
        // Arrange
        var minOrderAmount = new Money(50.00m, "USD");
        var coupon = CreateValidCoupon(minOrderAmount: minOrderAmount);
        var orderItems = CreateOrderItems((30.00m, 1, null, null)); // Below minimum
        var subtotal = new Money(30.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.MinAmountNotMet.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithMinOrderAmountNotMetByOneCent_ReturnsFailure()
    {
        // Arrange
        var minOrderAmount = new Money(25.00m, "USD");
        var coupon = CreateValidCoupon(minOrderAmount: minOrderAmount);
        var orderItems = CreateOrderItems((24.99m, 1, null, null)); // Just below minimum
        var subtotal = new Money(24.99m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.MinAmountNotMet.Code);
    }

    #endregion

    #region Coupon Applicability Tests

    [Test]
    public void ValidateAndCalculateDiscount_WithSpecificItemsNotInOrder_ReturnsFailure()
    {
        // Arrange
        var targetItemId = MenuItemId.CreateUnique();
        var differentItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(20.00m, 1, menuItemId: differentItemId) // Different item
        };
        var coupon = CreatePercentageCoupon(10m, CouponScope.SpecificItems, 
            new List<MenuItemId> { targetItemId });
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.NotApplicable.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithSpecificCategoriesNotInOrder_ReturnsFailure()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var differentCategoryId = MenuCategoryId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(20.00m, 1, categoryId: differentCategoryId) // Different category
        };
        var coupon = CreatePercentageCoupon(15m, CouponScope.SpecificCategories,
            categoryIds: new List<MenuCategoryId> { targetCategoryId });
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.NotApplicable.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithFreeItemNotInOrder_ReturnsFailure()
    {
        // Arrange
        var freeItemId = MenuItemId.CreateUnique();
        var differentItemId = MenuItemId.CreateUnique();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(20.00m, 1, menuItemId: differentItemId) // Different item
        };
        var coupon = CreateFreeItemCoupon(freeItemId);
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.NotApplicable.Code);
    }

    #endregion

    #region Edge Cases and Invalid States

    [Test]
    public void ValidateAndCalculateDiscount_WithEmptyOrderItems_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var orderItems = new List<OrderItem>(); // Empty order
        var subtotal = Money.Zero("USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.NotApplicable.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithZeroSubtotal_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(0.00m, 1) // Zero price item
        };
        var subtotal = Money.Zero("USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.NotApplicable.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithInvalidCouponType_ReturnsFailure()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");
        
        // Use reflection to create a CouponValue with an invalid coupon type
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        
        // Try to find the auto-implemented property backing field for Type in CouponValue
        var typeField = typeof(CouponValue)
            .GetField("<Type>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (typeField != null)
        {
            typeField.SetValue(couponValue, (CouponType)999); // Invalid enum value
        }
        
        // Now set this modified CouponValue to the Coupon using reflection
        var couponValueField = typeof(YummyZoom.Domain.CouponAggregate.Coupon)
            .GetField("<Value>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        couponValueField?.SetValue(coupon, couponValue);

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.InvalidType.Code);
    }

    #endregion

    #region Multiple Validation Failures (First Failure Wins)

    [Test]
    public void ValidateAndCalculateDiscount_WithMultipleValidationFailures_ReturnsFirstFailure()
    {
        // Arrange - Coupon that is both disabled AND expired
        var validStartDate = _fixedDateTime.AddDays(-30); // Started 30 days ago
        var expiredEndDate = _fixedDateTime.AddDays(-1); // Expired yesterday
        var coupon = CreateValidCoupon(
            isEnabled: false, // First check: disabled
            validityStartDate: validStartDate,
            validityEndDate: expiredEndDate); // Second check: expired
        var orderItems = CreateOrderItems((20.00m, 1, null, null));
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponDisabled.Code);
    }

    [Test]
    public void ValidateAndCalculateDiscount_WithUsageLimitAndMinAmountFailures_ReturnsUsageLimitFailure()
    {
        // Arrange - Coupon with usage limit exceeded AND min amount not met
        var minOrderAmount = new Money(100.00m, "USD");
        var coupon = CreateValidCoupon(
            totalUsageLimit: 5,
            currentTotalUsageCount: 5, // Usage limit exceeded (checked first)
            minOrderAmount: minOrderAmount);
        var orderItems = CreateOrderItems((20.00m, 1, null, null)); // Below min amount
        var subtotal = new Money(20.00m, "USD");

        // Act
        var result = _orderFinancialService.ValidateAndCalculateDiscount(
            coupon, 0, orderItems, subtotal, _fixedDateTime);

        // Assert
        result.ShouldBeFailure(CouponErrors.UsageLimitExceeded.Code);
    }

    #endregion
}
