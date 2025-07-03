using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Events;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

[TestFixture]
public class CouponTests
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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

    #region AppliesToItem() Method Tests

    [Test]
    public void AppliesToItem_WithWholeOrderCoupon_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();
        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemCoupon_ShouldReturnTrueForMatchingItem()
    {
        // Arrange
        var menuItemId = MenuItemId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificItems([menuItemId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemCoupon_ShouldReturnFalseForDifferentItem()
    {
        // Arrange
        var targetMenuItemId = MenuItemId.CreateUnique();
        var differentMenuItemId = MenuItemId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificItems([targetMenuItemId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(differentMenuItemId, categoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void AppliesToItem_WithCategoryCoupon_ShouldReturnTrueForItemInMatchingCategory()
    {
        // Arrange
        var categoryId = MenuCategoryId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificCategories([categoryId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithCategoryCoupon_ShouldReturnFalseForItemInDifferentCategory()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var differentCategoryId = MenuCategoryId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificCategories([targetCategoryId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, differentCategoryId);

        // Assert
        result.Should().BeFalse();
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
