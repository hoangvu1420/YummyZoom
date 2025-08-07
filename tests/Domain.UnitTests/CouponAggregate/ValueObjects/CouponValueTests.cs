using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate.ValueObjects;

[TestFixture]
public class CouponValueTests
{
    #region CreatePercentage() Method Tests

    [Test]
    public void CreatePercentage_WithValidPercentage_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange
        var percentage = 15.5m;

        // Act
        var result = CouponValue.CreatePercentage(percentage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var couponValue = result.Value;
        
        couponValue.Type.Should().Be(CouponType.Percentage);
        couponValue.PercentageValue.Should().Be(percentage);
        couponValue.FixedAmountValue.Should().BeNull();
        couponValue.FreeItemValue.Should().BeNull();
    }

    [TestCase(0.01)]
    [TestCase(1)]
    [TestCase(25)]
    [TestCase(50)]
    [TestCase(99.99)]
    [TestCase(100)]
    public void CreatePercentage_WithValidPercentageValues_ShouldSucceed(decimal percentage)
    {
        // Arrange & Act
        var result = CouponValue.CreatePercentage(percentage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PercentageValue.Should().Be(percentage);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-10.5)]
    public void CreatePercentage_WithZeroOrNegativePercentage_ShouldFailWithValidationError(decimal invalidPercentage)
    {
        // Arrange & Act
        var result = CouponValue.CreatePercentage(invalidPercentage);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.InvalidPercentageZeroOrNegative);
    }

    [TestCase(100.01)]
    [TestCase(150)]
    [TestCase(200)]
    public void CreatePercentage_WithPercentageOver100_ShouldFailWithValidationError(decimal invalidPercentage)
    {
        // Arrange & Act
        var result = CouponValue.CreatePercentage(invalidPercentage);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.InvalidPercentageExceedsMaximum);
    }

    #endregion

    #region CreateFixedAmount() Method Tests

    [Test]
    public void CreateFixedAmount_WithValidAmount_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange
        var amount = new Money(25.00m, Currencies.Default);

        // Act
        var result = CouponValue.CreateFixedAmount(amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var couponValue = result.Value;
        
        couponValue.Type.Should().Be(CouponType.FixedAmount);
        couponValue.PercentageValue.Should().BeNull();
        couponValue.FixedAmountValue.Should().Be(amount);
        couponValue.FreeItemValue.Should().BeNull();
    }

    [TestCase(0.01)]
    [TestCase(1)]
    [TestCase(10.99)]
    [TestCase(100)]
    [TestCase(500.75)]
    public void CreateFixedAmount_WithValidAmountValues_ShouldSucceed(decimal amountValue)
    {
        // Arrange
        var amount = new Money(amountValue, Currencies.Default);

        // Act
        var result = CouponValue.CreateFixedAmount(amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FixedAmountValue.Should().Be(amount);
    }

    [Test]
    public void CreateFixedAmount_WithZeroAmount_ShouldFailWithValidationError()
    {
        // Arrange
        var zeroAmount = new Money(0m, Currencies.Default);

        // Act
        var result = CouponValue.CreateFixedAmount(zeroAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.InvalidAmount);
    }

    [Test]
    public void CreateFixedAmount_WithNegativeAmount_ShouldFailWithValidationError()
    {
        // Arrange
        var negativeAmount = new Money(-10.50m, Currencies.Default);

        // Act
        var result = CouponValue.CreateFixedAmount(negativeAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.InvalidAmount);
    }

    #endregion

    #region CreateFreeItem() Method Tests

    [Test]
    public void CreateFreeItem_WithValidMenuItemId_ShouldSucceedAndSetCorrectProperties()
    {
        // Arrange
        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = CouponValue.CreateFreeItem(menuItemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var couponValue = result.Value;
        
        couponValue.Type.Should().Be(CouponType.FreeItem);
        couponValue.PercentageValue.Should().BeNull();
        couponValue.FixedAmountValue.Should().BeNull();
        couponValue.FreeItemValue.Should().Be(menuItemId);
    }

    [Test]
    public void CreateFreeItem_WithAnyMenuItemId_ShouldAlwaysSucceed()
    {
        // Arrange
        var menuItemId1 = MenuItemId.CreateUnique();
        var menuItemId2 = MenuItemId.CreateUnique();
        var menuItemId3 = MenuItemId.Create(Guid.Empty);

        // Act
        var result1 = CouponValue.CreateFreeItem(menuItemId1);
        var result2 = CouponValue.CreateFreeItem(menuItemId2);
        var result3 = CouponValue.CreateFreeItem(menuItemId3);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        
        result1.Value.FreeItemValue.Should().Be(menuItemId1);
        result2.Value.FreeItemValue.Should().Be(menuItemId2);
        result3.Value.FreeItemValue.Should().Be(menuItemId3);
    }

    #endregion

    #region GetDisplayValue() Method Tests

    [Test]
    public void GetDisplayValue_ForPercentageCoupon_ShouldReturnCorrectFormat()
    {
        // Arrange
        var couponValue = CouponValue.CreatePercentage(15.5m).Value;

        // Act
        var displayValue = couponValue.GetDisplayValue();

        // Assert
        displayValue.Should().Be("15.5% off");
    }

    [Test]
    public void GetDisplayValue_ForFixedAmountCoupon_ShouldReturnCorrectFormat()
    {
        // Arrange
        var amount = new Money(25.00m, Currencies.Default);
        var couponValue = CouponValue.CreateFixedAmount(amount).Value;

        // Act
        var displayValue = couponValue.GetDisplayValue();

        // Assert
        displayValue.Should().Be("25.00 USD off");
    }

    [Test]
    public void GetDisplayValue_ForFreeItemCoupon_ShouldReturnCorrectFormat()
    {
        // Arrange
        var menuItemId = MenuItemId.CreateUnique();
        var couponValue = CouponValue.CreateFreeItem(menuItemId).Value;

        // Act
        var displayValue = couponValue.GetDisplayValue();

        // Assert
        displayValue.Should().Be($"Free item (ID: {menuItemId.Value})");
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSamePercentageValues_ShouldBeEqual()
    {
        // Arrange
        var percentage = 15.5m;
        var couponValue1 = CouponValue.CreatePercentage(percentage).Value;
        var couponValue2 = CouponValue.CreatePercentage(percentage).Value;

        // Act & Assert
        couponValue1.Should().Be(couponValue2);
        couponValue1.Equals(couponValue2).Should().BeTrue();
        couponValue1.GetHashCode().Should().Be(couponValue2.GetHashCode());
    }

    [Test]
    public void Equality_WithSameFixedAmountValues_ShouldBeEqual()
    {
        // Arrange
        var amount = new Money(25.00m, Currencies.Default);
        var couponValue1 = CouponValue.CreateFixedAmount(amount).Value;
        var couponValue2 = CouponValue.CreateFixedAmount(amount).Value;

        // Act & Assert
        couponValue1.Should().Be(couponValue2);
        couponValue1.Equals(couponValue2).Should().BeTrue();
        couponValue1.GetHashCode().Should().Be(couponValue2.GetHashCode());
    }

    [Test]
    public void Equality_WithSameFreeItemValues_ShouldBeEqual()
    {
        // Arrange
        var menuItemId = MenuItemId.CreateUnique();
        var couponValue1 = CouponValue.CreateFreeItem(menuItemId).Value;
        var couponValue2 = CouponValue.CreateFreeItem(menuItemId).Value;

        // Act & Assert
        couponValue1.Should().Be(couponValue2);
        couponValue1.Equals(couponValue2).Should().BeTrue();
        couponValue1.GetHashCode().Should().Be(couponValue2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentPercentageValues_ShouldNotBeEqual()
    {
        // Arrange
        var couponValue1 = CouponValue.CreatePercentage(10m).Value;
        var couponValue2 = CouponValue.CreatePercentage(15m).Value;

        // Act & Assert
        couponValue1.Should().NotBe(couponValue2);
        couponValue1.Equals(couponValue2).Should().BeFalse();
    }

    [Test]
    public void Equality_WithDifferentTypes_ShouldNotBeEqual()
    {
        // Arrange
        var percentageCoupon = CouponValue.CreatePercentage(10m).Value;
        var fixedAmountCoupon = CouponValue.CreateFixedAmount(new Money(10m, Currencies.Default)).Value;
        var freeItemCoupon = CouponValue.CreateFreeItem(MenuItemId.CreateUnique()).Value;

        // Act & Assert
        percentageCoupon.Should().NotBe(fixedAmountCoupon);
        percentageCoupon.Should().NotBe(freeItemCoupon);
        fixedAmountCoupon.Should().NotBe(freeItemCoupon);
    }

    [Test]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var couponValue = CouponValue.CreatePercentage(10m).Value;

        // Act & Assert
        couponValue.Equals(null).Should().BeFalse();
    }

    #endregion
}
