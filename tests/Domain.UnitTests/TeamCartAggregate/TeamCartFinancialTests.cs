using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartFinancialTests
{
    private TeamCart _teamCart = null!;
    private UserId _hostUserId = null!;
    private UserId _guestUserId = null!;
    private RestaurantId _restaurantId = null!;
    private MenuItemId _menuItemId1 = null!;
    private MenuItemId _menuItemId2 = null!;
    private MenuCategoryId _menuCategoryId = null!;

    [SetUp]
    public void SetUp()
    {
        _hostUserId = UserId.CreateUnique();
        _guestUserId = UserId.CreateUnique();
        _restaurantId = RestaurantId.CreateUnique();
        _menuItemId1 = MenuItemId.CreateUnique();
        _menuItemId2 = MenuItemId.CreateUnique();
        _menuCategoryId = MenuCategoryId.CreateUnique();

        // Create a team cart and add members
        _teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        _teamCart.AddMember(_guestUserId, "Guest User");

        // Add items to the cart
        _teamCart.AddItem(
            _hostUserId,
            _menuItemId1,
            _menuCategoryId,
            "Test Item 1",
            new Money(10.00m, Currencies.Default),
            1);

        _teamCart.AddItem(
            _guestUserId,
            _menuItemId2,
            _menuCategoryId,
            "Test Item 2",
            new Money(15.00m, Currencies.Default),
            1);

        // Initiate checkout to move to AwaitingPayments status
        _teamCart.InitiateCheckout(_hostUserId);

        // Add payment commitments
        _teamCart.CommitToCashOnDelivery(_hostUserId, new Money(10.00m, Currencies.Default));
        _teamCart.RecordSuccessfulOnlinePayment(_guestUserId, new Money(15.00m, Currencies.Default), "txn_123");
    }

    #region ApplyTip Tests

    [Test]
    public void ApplyTip_WhenHostRequests_ShouldSucceed()
    {
        // Arrange
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = _teamCart.ApplyTip(_hostUserId, tipAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.TipAmount.Should().Be(tipAmount);
    }

    [Test]
    public void ApplyTip_WhenNonHostRequests_ShouldFail()
    {
        // Arrange
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = _teamCart.ApplyTip(_guestUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanModifyFinancials);
        _teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyTip_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var tipAmount = new Money(-5.00m, Currencies.Default);

        // Act
        var result = _teamCart.ApplyTip(_hostUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidTip);
        _teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyTip_InOpenStatus_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = teamCart.ApplyTip(_hostUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotModifyFinancialsInCurrentStatus);
        teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyTip_InReadyToConfirmStatus_ShouldSucceed()
    {
        // Arrange - Set up a cart in ReadyToConfirm status
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = _teamCart.ApplyTip(_hostUserId, tipAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.TipAmount.Should().Be(tipAmount);
    }

    [Test]
    public void ApplyTip_CanUpdateExistingTip()
    {
        // Arrange
        var initialTip = new Money(5.00m, Currencies.Default);
        var updatedTip = new Money(7.50m, Currencies.Default);

        // Act
        _teamCart.ApplyTip(_hostUserId, initialTip);
        var result = _teamCart.ApplyTip(_hostUserId, updatedTip);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.TipAmount.Should().Be(updatedTip);
    }

    #endregion

    #region ApplyCoupon Tests

    [Test]
    public void ApplyCoupon_WhenHostRequests_ShouldSucceed()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m);
    }

    [Test]
    public void ApplyCoupon_WhenNonHostRequests_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_guestUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanModifyFinancials);
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyCoupon_WhenCouponAlreadyApplied_ShouldFail()
    {
        // Arrange
        var couponId1 = CouponId.CreateUnique();
        var couponId2 = CouponId.CreateUnique();
        var couponValue1 = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var couponValue2 = CouponValue.CreateFixedAmount(new Money(10.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Apply first coupon
        _teamCart.ApplyCoupon(_hostUserId, couponId1, couponValue1, appliesTo, null);

        // Act - Try to apply second coupon
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId2, couponValue2, appliesTo, null);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CouponAlreadyApplied);
        _teamCart.AppliedCouponId.Should().Be(couponId1); // First coupon should still be applied
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m); // Discount should remain from first coupon
    }

    [Test]
    public void ApplyCoupon_WithInsufficientSubtotal_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        var minOrderAmount = new Money(50.00m, Currencies.Default); // Higher than cart total

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, minOrderAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CouponNotApplicable);
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyCoupon_WithValidFixedAmountCoupon_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m);
    }

    [Test]
    public void ApplyCoupon_WithValidPercentageCoupon_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(20).Value; // 20% off
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m); // 20% of 25.00 = 5.00
    }

    [Test]
    public void ApplyCoupon_WithSpecificItemScope_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(50).Value; // 50% off
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { _menuItemId1 }).Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m); // 50% of 10.00 = 5.00
    }

    [Test]
    public void ApplyCoupon_WithSpecificCategoryScope_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(40).Value; // 40% off
        var appliesTo = AppliesTo.CreateForSpecificCategories(new List<MenuCategoryId> { _menuCategoryId }).Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(10.00m); // 40% of 25.00 = 10.00
    }

    [Test]
    public void ApplyCoupon_WithFreeItemCoupon_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFreeItem(_menuItemId1).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.DiscountAmount.Amount.Should().Be(10.00m); // Free item worth 10.00
    }

    [Test]
    public void ApplyCoupon_WithFreeItemNotInCart_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var nonExistentItemId = MenuItemId.CreateUnique();
        var couponValue = CouponValue.CreateFreeItem(nonExistentItemId).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CouponNotApplicable);
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    #endregion

    #region RemoveCoupon Tests

    [Test]
    public void RemoveCoupon_WhenCouponApplied_ShouldRemoveCouponAndResetDiscount()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Apply coupon first
        _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Act
        var result = _teamCart.RemoveCoupon(_hostUserId);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void RemoveCoupon_WhenNoCouponApplied_ShouldSucceed()
    {
        // Act
        var result = _teamCart.RemoveCoupon(_hostUserId);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void RemoveCoupon_WhenNonHostRequests_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Apply coupon first
        _teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Act
        var result = _teamCart.RemoveCoupon(_guestUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanModifyFinancials);
        _teamCart.AppliedCouponId.Should().Be(couponId); // Coupon should still be applied
        _teamCart.DiscountAmount.Amount.Should().Be(5.00m); // Discount should remain
    }

    #endregion
}
