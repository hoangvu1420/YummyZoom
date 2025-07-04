using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

[TestFixture]
public class CouponUpdateMethodsTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultCode = "SAVE10";
    private const string DefaultDescription = "Save 10% on your order";
    private static readonly CouponValue DefaultValue = CouponValue.CreatePercentage(10m).Value;
    private static readonly AppliesTo DefaultAppliesTo = AppliesTo.CreateForWholeOrder().Value;
    private static readonly DateTime DefaultStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEndDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    #region UpdateDescription() Method Tests

    [Test]
    public void UpdateDescription_WithValidDescription_ShouldSucceedAndUpdateDescription()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var newDescription = "New promotional description";

        // Act
        var result = coupon.UpdateDescription(newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.Description.Should().Be(newDescription);
    }

    [Test]
    public void UpdateDescription_WithDescriptionContainingWhitespace_ShouldTrimDescription()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var descriptionWithWhitespace = "  New promotional description  ";

        // Act
        var result = coupon.UpdateDescription(descriptionWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.Description.Should().Be("New promotional description");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDescription_WithNullOrEmptyDescription_ShouldFailWithCouponDescriptionEmptyError(string invalidDescription)
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var originalDescription = coupon.Description;

        // Act
        var result = coupon.UpdateDescription(invalidDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponDescriptionEmpty);
        coupon.Description.Should().Be(originalDescription); // No change
    }

    [Test]
    public void UpdateDescription_WithNullDescription_ShouldFailWithCouponDescriptionEmptyError()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var originalDescription = coupon.Description;

        // Act
#pragma warning disable CS8625
        var result = coupon.UpdateDescription(null);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponDescriptionEmpty);
        coupon.Description.Should().Be(originalDescription);
    }

    #endregion

    #region SetMinimumOrderAmount() Method Tests

    [Test]
    public void SetMinimumOrderAmount_WithValidAmount_ShouldSucceedAndSetAmount()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var minAmount = new Money(25.00m, Currencies.Default);

        // Act
        var result = coupon.SetMinimumOrderAmount(minAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.MinOrderAmount.Should().Be(minAmount);
    }

    [Test]
    public void SetMinimumOrderAmount_WithNullAmount_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.SetMinimumOrderAmount(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.MinOrderAmount.Should().BeNull();
    }

    [Test]
    public void SetMinimumOrderAmount_WithNegativeAmount_ShouldFailWithInvalidMinOrderAmountError()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var negativeAmount = new Money(-10.00m, Currencies.Default);
        var originalAmount = coupon.MinOrderAmount;

        // Act
        var result = coupon.SetMinimumOrderAmount(negativeAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidMinOrderAmount);
        coupon.MinOrderAmount.Should().Be(originalAmount); // No change
    }

    [Test]
    public void SetMinimumOrderAmount_WithZeroAmount_ShouldFailWithInvalidMinOrderAmountError()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var zeroAmount = new Money(0m, Currencies.Default);

        // Act
        var result = coupon.SetMinimumOrderAmount(zeroAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidMinOrderAmount);
    }

    #endregion

    #region RemoveMinimumOrderAmount() Method Tests

    [Test]
    public void RemoveMinimumOrderAmount_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        coupon.SetMinimumOrderAmount(new Money(25.00m, Currencies.Default)); // Set first

        // Act
        var result = coupon.RemoveMinimumOrderAmount();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.MinOrderAmount.Should().BeNull();
    }

    [Test]
    public void RemoveMinimumOrderAmount_WhenAlreadyNull_ShouldSucceed()
    {
        // Arrange
        var coupon = CreateValidCoupon(); // MinOrderAmount is null by default

        // Act
        var result = coupon.RemoveMinimumOrderAmount();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.MinOrderAmount.Should().BeNull();
    }

    #endregion

    #region SetTotalUsageLimit() Method Tests

    [Test]
    public void SetTotalUsageLimit_WithValidLimit_ShouldSucceedAndSetLimit()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var newLimit = 100;

        // Act
        var result = coupon.SetTotalUsageLimit(newLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.TotalUsageLimit.Should().Be(newLimit);
    }

    [Test]
    public void SetTotalUsageLimit_WithNullLimit_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.SetTotalUsageLimit(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.TotalUsageLimit.Should().BeNull();
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void SetTotalUsageLimit_WithInvalidLimit_ShouldFailWithInvalidUsageLimitError(int invalidLimit)
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var originalLimit = coupon.TotalUsageLimit;

        // Act
        var result = coupon.SetTotalUsageLimit(invalidLimit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidUsageLimit);
        coupon.TotalUsageLimit.Should().Be(originalLimit); // No change
    }

    [Test]
    public void SetTotalUsageLimit_WithLimitLowerThanCurrentUsage_ShouldFailWithUsageCountCannotExceedLimitError()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var coupon = Coupon.Create(
            couponId,
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            currentTotalUsageCount: 5).Value; // Current usage is 5

        // Act
        var result = coupon.SetTotalUsageLimit(3); // Try to set limit lower than current usage

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.UsageCountCannotExceedLimit(5, 3));
        coupon.TotalUsageLimit.Should().BeNull(); // No change
    }

    [Test]
    public void SetTotalUsageLimit_WithLimitEqualToCurrentUsage_ShouldSucceed()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var coupon = Coupon.Create(
            couponId,
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            currentTotalUsageCount: 5).Value;

        // Act
        var result = coupon.SetTotalUsageLimit(5); // Equal to current usage

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.TotalUsageLimit.Should().Be(5);
    }

    #endregion

    #region RemoveTotalUsageLimit() Method Tests

    [Test]
    public void RemoveTotalUsageLimit_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        coupon.SetTotalUsageLimit(100); // Set first

        // Act
        var result = coupon.RemoveTotalUsageLimit();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.TotalUsageLimit.Should().BeNull();
    }

    [Test]
    public void RemoveTotalUsageLimit_WhenAlreadyNull_ShouldSucceed()
    {
        // Arrange
        var coupon = CreateValidCoupon(); // TotalUsageLimit is null by default

        // Act
        var result = coupon.RemoveTotalUsageLimit();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.TotalUsageLimit.Should().BeNull();
    }

    #endregion

    #region SetPerUserUsageLimit() Method Tests

    [Test]
    public void SetPerUserUsageLimit_WithValidLimit_ShouldSucceedAndSetLimit()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var newLimit = 5;

        // Act
        var result = coupon.SetPerUserUsageLimit(newLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.UsageLimitPerUser.Should().Be(newLimit);
    }

    [Test]
    public void SetPerUserUsageLimit_WithNullLimit_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.SetPerUserUsageLimit(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.UsageLimitPerUser.Should().BeNull();
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void SetPerUserUsageLimit_WithInvalidLimit_ShouldFailWithInvalidPerUserLimitError(int invalidLimit)
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var originalLimit = coupon.UsageLimitPerUser;

        // Act
        var result = coupon.SetPerUserUsageLimit(invalidLimit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidPerUserLimit);
        coupon.UsageLimitPerUser.Should().Be(originalLimit); // No change
    }

    #endregion

    #region RemovePerUserUsageLimit() Method Tests

    [Test]
    public void RemovePerUserUsageLimit_ShouldSucceedAndSetToNull()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        coupon.SetPerUserUsageLimit(5); // Set first

        // Act
        var result = coupon.RemovePerUserUsageLimit();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.UsageLimitPerUser.Should().BeNull();
    }

    [Test]
    public void RemovePerUserUsageLimit_WhenAlreadyNull_ShouldSucceed()
    {
        // Arrange
        var coupon = CreateValidCoupon(); // UsageLimitPerUser is null by default

        // Act
        var result = coupon.RemovePerUserUsageLimit();

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.UsageLimitPerUser.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static Coupon CreateValidCoupon()
    {
        return Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;
    }

    #endregion
}
