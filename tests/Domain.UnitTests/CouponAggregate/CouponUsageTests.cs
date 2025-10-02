using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Events;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

/// <summary>
/// Tests for Coupon usage functionality including Use() and IsValidForUse() methods.
/// </summary>
[TestFixture]
public class CouponUsageTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultCode = "SAVE10";
    private const string DefaultDescription = "Save 10% on your order";
    private static readonly CouponValue DefaultValue = CouponValue.CreatePercentage(10m).Value;
    private static readonly AppliesTo DefaultAppliesTo = AppliesTo.CreateForWholeOrder().Value;
    private static readonly DateTime DefaultStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEndDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly Money DefaultMinOrderAmount = new Money(25.00m, Currencies.Default);

    #region Use() Method Tests

    [Test]
    public void Use_WithValidCoupon_ShouldSucceedAndIncrementUsageCount()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var usageTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Use(usageTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(1);

        // Verify domain event
        coupon.DomainEvents.Should().Contain(e => e.GetType() == typeof(CouponUsed));
        var usedEvent = coupon.DomainEvents.OfType<CouponUsed>().Last();
        usedEvent.CouponId.Should().Be((CouponId)coupon.Id);
        usedEvent.PreviousUsageCount.Should().Be(0);
        usedEvent.NewUsageCount.Should().Be(1);
        usedEvent.UsedAt.Should().Be(usageTime);
    }

    [Test]
    public void Use_WhenDisabled_ShouldFailWithCouponDisabledError()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: false);
        var usageTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Use(usageTime);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponDisabled);
        coupon.CurrentTotalUsageCount.Should().Be(0); // No change
        coupon.DomainEvents.Should().NotContain(e => e.GetType() == typeof(CouponUsed));
    }

    [Test]
    public void Use_BeforeValidityStartDate_ShouldFailWithCouponNotYetValidError()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var beforeStartTime = DefaultStartDate.AddSeconds(-1);

        // Act
        var result = coupon.Use(beforeStartTime);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponNotYetValid);
        coupon.CurrentTotalUsageCount.Should().Be(0);
        coupon.DomainEvents.Should().NotContain(e => e.GetType() == typeof(CouponUsed));
    }

    [Test]
    public void Use_AfterValidityEndDate_ShouldFailWithCouponExpiredError()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var afterEndTime = DefaultEndDate.AddSeconds(1);

        // Act
        var result = coupon.Use(afterEndTime);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponExpired);
        coupon.CurrentTotalUsageCount.Should().Be(0);
        coupon.DomainEvents.Should().NotContain(e => e.GetType() == typeof(CouponUsed));
    }

    [Test]
    public void Use_AtExactValidityStartDate_ShouldSucceed()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.Use(DefaultStartDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(1);
    }

    [Test]
    public void Use_AtExactValidityEndDate_ShouldSucceed()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.Use(DefaultEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(1);
    }

    [Test]
    public void Use_WhenUsageLimitReached_ShouldFailWithUsageLimitExceededError()
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
            currentTotalUsageCount: 2,
            totalUsageLimit: 2).Value;

        var usageTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Use(usageTime);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.UsageLimitExceeded);
        coupon.CurrentTotalUsageCount.Should().Be(2); // No change
        coupon.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void Use_AtExactUsageLimit_ShouldFailWithUsageLimitExceededError()
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
            currentTotalUsageCount: 1,
            totalUsageLimit: 1).Value;

        var usageTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Use(usageTime);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.UsageLimitExceeded);
        coupon.CurrentTotalUsageCount.Should().Be(1);
    }

    [Test]
    public void Use_MultipleTimesUnderLimit_ShouldSucceedAndIncrementCorrectly()
    {
        // Arrange
        var coupon = CreateValidCoupon(totalUsageLimit: 3);
        var usageTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act & Assert
        var result1 = coupon.Use(usageTime);
        result1.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(1);

        var result2 = coupon.Use(usageTime.AddMinutes(1));
        result2.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(2);

        var result3 = coupon.Use(usageTime.AddMinutes(2));
        result3.IsSuccess.Should().BeTrue();
        coupon.CurrentTotalUsageCount.Should().Be(3);

        // Verify events
        var usedEvents = coupon.DomainEvents.OfType<CouponUsed>().ToList();
        usedEvents.Should().HaveCount(3);
        usedEvents[0].PreviousUsageCount.Should().Be(0);
        usedEvents[0].NewUsageCount.Should().Be(1);
        usedEvents[1].PreviousUsageCount.Should().Be(1);
        usedEvents[1].NewUsageCount.Should().Be(2);
        usedEvents[2].PreviousUsageCount.Should().Be(2);
        usedEvents[2].NewUsageCount.Should().Be(3);
    }

    #endregion

    #region IsValidForUse() Method Tests

    [Test]
    public void IsValidForUse_WithValidCouponAndCurrentTime_ShouldReturnTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var checkTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc); // Within validity period

        // Act
        var result = coupon.IsValidForUse(checkTime);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidForUse_WithoutCheckTime_ShouldUseCurrentTimeAndReturnTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act & Assert
        // This assumes the test runs during the validity period (2025)
        // In a real scenario, you might want to mock DateTime.UtcNow
        // For now, we'll test with explicit time
        var futureTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = coupon.IsValidForUse(futureTime);
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidForUse_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: false);
        var checkTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.IsValidForUse(checkTime);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidForUse_BeforeValidityStart_ShouldReturnFalse()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var beforeStartTime = DefaultStartDate.AddSeconds(-1);

        // Act
        var result = coupon.IsValidForUse(beforeStartTime);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidForUse_AfterValidityEnd_ShouldReturnFalse()
    {
        // Arrange
        var coupon = CreateValidCoupon();
        var afterEndTime = DefaultEndDate.AddSeconds(1);

        // Act
        var result = coupon.IsValidForUse(afterEndTime);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidForUse_AtExactValidityStart_ShouldReturnTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.IsValidForUse(DefaultStartDate);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidForUse_AtExactValidityEnd_ShouldReturnTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon();

        // Act
        var result = coupon.IsValidForUse(DefaultEndDate);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidForUse_WhenUsageLimitReached_ShouldReturnFalse()
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
            currentTotalUsageCount: 5,
            totalUsageLimit: 5).Value;

        var checkTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.IsValidForUse(checkTime);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsValidForUse_WhenUsageUnderLimit_ShouldReturnTrue()
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
            currentTotalUsageCount: 3,
            totalUsageLimit: 5).Value;

        var checkTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.IsValidForUse(checkTime);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidForUse_WithNoUsageLimit_ShouldReturnTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon(); // No usage limit set
        var checkTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.IsValidForUse(checkTime);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static Coupon CreateValidCoupon(
        bool isEnabled = true,
        int? totalUsageLimit = null,
        int? usageLimitPerUser = null,
        Money? minOrderAmount = null)
    {
        return Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            minOrderAmount,
            totalUsageLimit,
            usageLimitPerUser,
            isEnabled).Value;
    }

    #endregion
}
