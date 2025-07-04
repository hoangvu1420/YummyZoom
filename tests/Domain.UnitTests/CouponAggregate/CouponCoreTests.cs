using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Events;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

/// <summary>
/// Tests for core Coupon aggregate functionality including creation, enable/disable operations.
/// </summary>
[TestFixture]
public class CouponCoreTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultCode = "SAVE10";
    private const string DefaultDescription = "Save 10% on your order";
    private static readonly CouponValue DefaultValue = CouponValue.CreatePercentage(10m).Value;
    private static readonly AppliesTo DefaultAppliesTo = AppliesTo.CreateForWholeOrder().Value;
    private static readonly DateTime DefaultStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEndDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly Money DefaultMinOrderAmount = new Money(25.00m, Currencies.Default);

    #region Create() Method Tests - New Coupon

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCouponCorrectly()
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var coupon = result.Value;
        
        coupon.Id.Value.Should().NotBe(Guid.Empty);
        coupon.RestaurantId.Should().Be(DefaultRestaurantId);
        coupon.Code.Should().Be(DefaultCode);
        coupon.Description.Should().Be(DefaultDescription);
        coupon.Value.Should().Be(DefaultValue);
        coupon.AppliesTo.Should().Be(DefaultAppliesTo);
        coupon.ValidityStartDate.Should().Be(DefaultStartDate);
        coupon.ValidityEndDate.Should().Be(DefaultEndDate);
        coupon.MinOrderAmount.Should().BeNull();
        coupon.TotalUsageLimit.Should().BeNull();
        coupon.CurrentTotalUsageCount.Should().Be(0);
        coupon.IsEnabled.Should().BeTrue();
        coupon.UsageLimitPerUser.Should().BeNull();

        // Verify domain event
        coupon.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(CouponCreated));
        var createdEvent = coupon.DomainEvents.OfType<CouponCreated>().Single();
        createdEvent.CouponId.Should().Be((CouponId)coupon.Id);
        createdEvent.RestaurantId.Should().Be(DefaultRestaurantId);
        createdEvent.Code.Should().Be(DefaultCode);
        createdEvent.Type.Should().Be(CouponType.Percentage);
        createdEvent.ValidityStartDate.Should().Be(DefaultStartDate);
        createdEvent.ValidityEndDate.Should().Be(DefaultEndDate);
    }

    [Test]
    public void Create_WithAllOptionalParameters_ShouldSucceedAndSetAllProperties()
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            DefaultMinOrderAmount,
            totalUsageLimit: 100,
            usageLimitPerUser: 5,
            isEnabled: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var coupon = result.Value;
        
        coupon.MinOrderAmount.Should().Be(DefaultMinOrderAmount);
        coupon.TotalUsageLimit.Should().Be(100);
        coupon.UsageLimitPerUser.Should().Be(5);
        coupon.IsEnabled.Should().BeFalse();
    }

    [Test]
    public void Create_WithCodeContainingWhitespace_ShouldTrimAndNormalizeCode()
    {
        // Arrange
        var codeWithWhitespace = "  save10  ";

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            codeWithWhitespace,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("SAVE10"); // Trimmed and uppercase
    }

    [Test]
    public void Create_WithDescriptionContainingWhitespace_ShouldTrimDescription()
    {
        // Arrange
        var descriptionWithWhitespace = "  Save 10% on your order  ";

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            descriptionWithWhitespace,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be(DefaultDescription);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyCode_ShouldFailWithCouponCodeEmptyError(string invalidCode)
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            invalidCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponCodeEmpty);
    }

    [Test]
    public void Create_WithNullCode_ShouldFailWithCouponCodeEmptyError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = Coupon.Create(
            DefaultRestaurantId,
            null,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponCodeEmpty);
    }

    [Test]
    public void Create_WithCodeTooLong_ShouldFailWithCouponCodeTooLongError()
    {
        // Arrange
        var longCode = new string('A', 51); // Max is 50

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            longCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponCodeTooLong(50));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrEmptyDescription_ShouldFailWithCouponDescriptionEmptyError(string invalidDescription)
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            invalidDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CouponDescriptionEmpty);
    }

    [Test]
    public void Create_WithInvalidValidityPeriod_ShouldFailWithInvalidValidityPeriodError()
    {
        // Arrange
        var startDate = new DateTime(2025, 12, 31);
        var endDate = new DateTime(2025, 1, 1); // End before start

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            startDate,
            endDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidValidityPeriod);
    }

    [Test]
    public void Create_WithEqualStartAndEndDates_ShouldFailWithInvalidValidityPeriodError()
    {
        // Arrange
        var sameDate = new DateTime(2025, 6, 15);

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            sameDate,
            sameDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidValidityPeriod);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void Create_WithInvalidTotalUsageLimit_ShouldFailWithInvalidUsageLimitError(int invalidLimit)
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            totalUsageLimit: invalidLimit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidUsageLimit);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void Create_WithInvalidPerUserUsageLimit_ShouldFailWithInvalidPerUserLimitError(int invalidLimit)
    {
        // Arrange & Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            usageLimitPerUser: invalidLimit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidPerUserLimit);
    }

    [Test]
    public void Create_WithInvalidMinOrderAmount_ShouldFailWithInvalidMinOrderAmountError()
    {
        // Arrange
        var invalidAmount = new Money(-10.00m, Currencies.Default);

        // Act
        var result = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            invalidAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidMinOrderAmount);
    }

    #endregion

    #region Create() Method Tests - From Persistence

    [Test]
    public void Create_FromPersistence_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        
        // Act
        var result = Coupon.Create(
            couponId,
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            currentTotalUsageCount: 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var coupon = result.Value;
        
        coupon.Id.Should().Be(couponId);
        coupon.CurrentTotalUsageCount.Should().Be(5);
        coupon.DomainEvents.Should().BeEmpty(); // No events for persistence recreation
    }

    [Test]
    public void Create_FromPersistence_WithUsageCountExceedingLimit_ShouldFailWithUsageCountCannotExceedLimitError()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        
        // Act
        var result = Coupon.Create(
            couponId,
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            DefaultAppliesTo,
            DefaultStartDate,
            DefaultEndDate,
            currentTotalUsageCount: 15,
            totalUsageLimit: 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.UsageCountCannotExceedLimit(15, 10));
    }

    #endregion

    #region Enable() Method Tests

    [Test]
    public void Enable_WhenDisabled_ShouldSucceedAndSetEnabledToTrue()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: false);
        var enableTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Enable(enableTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.IsEnabled.Should().BeTrue();
        
        // Verify domain event
        coupon.DomainEvents.Should().Contain(e => e.GetType() == typeof(CouponEnabled));
        var enabledEvent = coupon.DomainEvents.OfType<CouponEnabled>().Single();
        enabledEvent.CouponId.Should().Be((CouponId)coupon.Id);
        enabledEvent.EnabledAt.Should().Be(enableTime);
    }

    [Test]
    public void Enable_WhenAlreadyEnabled_ShouldSucceedButNotRaiseEvent()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: true);
        var enableTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Enable(enableTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.IsEnabled.Should().BeTrue();
        coupon.DomainEvents.Should().NotContain(e => e.GetType() == typeof(CouponEnabled));
    }

    #endregion

    #region Disable() Method Tests

    [Test]
    public void Disable_WhenEnabled_ShouldSucceedAndSetEnabledToFalse()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: true);
        var disableTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Disable(disableTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.IsEnabled.Should().BeFalse();
        
        // Verify domain event
        coupon.DomainEvents.Should().Contain(e => e.GetType() == typeof(CouponDisabled));
        var disabledEvent = coupon.DomainEvents.OfType<CouponDisabled>().Single();
        disabledEvent.CouponId.Should().Be((CouponId)coupon.Id);
        disabledEvent.DisabledAt.Should().Be(disableTime);
    }

    [Test]
    public void Disable_WhenAlreadyDisabled_ShouldSucceedButNotRaiseEvent()
    {
        // Arrange
        var coupon = CreateValidCoupon(isEnabled: false);
        var disableTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = coupon.Disable(disableTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coupon.IsEnabled.Should().BeFalse();
        coupon.DomainEvents.Should().NotContain(e => e.GetType() == typeof(CouponDisabled));
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
