using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Domain.UnitTests.TeamCartAggregate.TeamCartTestHelpers;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartFinancialTests
{
    private TeamCart _teamCart = null!;
    private UserId _guestUserId = null!;
    private MenuItemId _menuItemId1 = null!;
    private MenuItemId _menuItemId2 = null!;
    private MenuCategoryId _menuCategoryId = null!;

    [SetUp]
    public void SetUp()
    {
        _guestUserId = UserId.CreateUnique();
        _menuItemId1 = MenuItemId.CreateUnique();
        _menuItemId2 = MenuItemId.CreateUnique();
        _menuCategoryId = MenuCategoryId.CreateUnique();

        // Create a team cart and add members
        _teamCart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        _teamCart.AddMember(_guestUserId, DefaultGuestName).ShouldBeSuccessful();

        // Add items to the cart
        _teamCart.AddItem(
            DefaultHostUserId,
            _menuItemId1,
            _menuCategoryId,
            "Test Item 1",
            new Money(10.00m, Currencies.Default),
            1).ShouldBeSuccessful();

        _teamCart.AddItem(
            _guestUserId,
            _menuItemId2,
            _menuCategoryId,
            "Test Item 2",
            new Money(15.00m, Currencies.Default),
            1).ShouldBeSuccessful();

        // Lock the cart for payment
        _teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
    }

    #region ApplyTip Tests

    [Test]
    public void ApplyTip_WhenHostRequests_ShouldSucceed()
    {
        // Arrange
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = _teamCart.ApplyTip(DefaultHostUserId, tipAmount);

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
        var result = _teamCart.ApplyTip(DefaultHostUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidTip);
        _teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyTip_InOpenStatus_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = teamCart.ApplyTip(DefaultHostUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
        teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default));
    }

    [Test]
    public void ApplyTip_InReadyToConfirmStatus_ShouldFail()
    {
        // Arrange - Set up a cart in ReadyToConfirm status
        var teamCart = CreateTeamCartReadyForConversion();
        var tipAmount = new Money(5.00m, Currencies.Default);

        // Act
        var result = teamCart.ApplyTip(DefaultHostUserId, tipAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void ApplyTip_CanUpdateExistingTip()
    {
        // Arrange
        var initialTip = new Money(5.00m, Currencies.Default);
        var updatedTip = new Money(7.50m, Currencies.Default);

        // Act
        _teamCart.ApplyTip(DefaultHostUserId, initialTip).ShouldBeSuccessful();
        var result = _teamCart.ApplyTip(DefaultHostUserId, updatedTip);

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

        // Act
        var result = _teamCart.ApplyCoupon(DefaultHostUserId, couponId);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().Be(couponId);
        _teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default)); // Tip should be reset
    }

    [Test]
    public void ApplyCoupon_WhenNonHostRequests_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();

        // Act
        var result = _teamCart.ApplyCoupon(_guestUserId, couponId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanModifyFinancials);
        _teamCart.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public void ApplyCoupon_WhenCouponAlreadyApplied_ShouldFail()
    {
        // Arrange
        var couponId1 = CouponId.CreateUnique();
        var couponId2 = CouponId.CreateUnique();

        // Apply first coupon
        _teamCart.ApplyCoupon(DefaultHostUserId, couponId1).ShouldBeSuccessful();

        // Act - Try to apply second coupon
        var result = _teamCart.ApplyCoupon(DefaultHostUserId, couponId2);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CouponAlreadyApplied);
        _teamCart.AppliedCouponId.Should().Be(couponId1); // First coupon should still be applied
    }

    [Test]
    public void ApplyCoupon_InOpenStatus_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        var couponId = CouponId.CreateUnique();

        // Act
        var result = teamCart.ApplyCoupon(DefaultHostUserId, couponId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
        teamCart.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public void ApplyCoupon_InReadyToConfirmStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        var couponId = CouponId.CreateUnique();

        // Act
        var result = teamCart.ApplyCoupon(DefaultHostUserId, couponId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void ApplyCoupon_InConvertedStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateConvertedTeamCart();
        var couponId = CouponId.CreateUnique();

        // Act
        var result = teamCart.ApplyCoupon(DefaultHostUserId, couponId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void ApplyCoupon_InExpiredStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateExpiredTeamCart();
        var couponId = CouponId.CreateUnique();

        // Act
        var result = teamCart.ApplyCoupon(DefaultHostUserId, couponId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    #endregion

    #region RemoveCoupon Tests

    [Test]
    public void RemoveCoupon_WhenCouponApplied_ShouldRemoveCoupon()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();

        // Apply coupon first
        _teamCart.ApplyCoupon(DefaultHostUserId, couponId).ShouldBeSuccessful();

        // Act
        var result = _teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().BeNull();
        _teamCart.TipAmount.Should().Be(Money.Zero(Currencies.Default)); // Tip should be reset
    }

    [Test]
    public void RemoveCoupon_WhenNoCouponApplied_ShouldSucceed()
    {
        // Act
        var result = _teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public void RemoveCoupon_WhenNonHostRequests_ShouldFail()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();

        // Apply coupon first
        _teamCart.ApplyCoupon(DefaultHostUserId, couponId).ShouldBeSuccessful();

        // Act
        var result = _teamCart.RemoveCoupon(_guestUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanModifyFinancials);
        _teamCart.AppliedCouponId.Should().Be(couponId); // Coupon should still be applied
    }

    [Test]
    public void RemoveCoupon_InOpenStatus_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        var couponId = CouponId.CreateUnique();
        // Note: ApplyCoupon in open status will fail, but this setup is for testing RemoveCoupon's behavior in that state.
        // The actual ApplyCoupon test for Open status already asserts failure.
        teamCart.ApplyCoupon(DefaultHostUserId, couponId); 

        // Act
        var result = teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void RemoveCoupon_InReadyToConfirmStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyForConversion();
        var couponId = CouponId.CreateUnique();
        
        // We need to set the AppliedCouponId directly since we can't apply a coupon in ReadyToConfirm status
        typeof(TeamCart).GetProperty("AppliedCouponId")?.SetValue(teamCart, couponId);

        // Act
        var result = teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void RemoveCoupon_InConvertedStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateConvertedTeamCart();
        var couponId = CouponId.CreateUnique();
        
        // We need to set the AppliedCouponId directly since we can't apply a coupon in Converted status
        typeof(TeamCart).GetProperty("AppliedCouponId")?.SetValue(teamCart, couponId);

        // Act
        var result = teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    [Test]
    public void RemoveCoupon_InExpiredStatus_ShouldFail()
    {
        // Arrange
        var teamCart = CreateExpiredTeamCart();
        var couponId = CouponId.CreateUnique();
        
        // We need to set the AppliedCouponId directly since we can't apply a coupon in Expired status
        typeof(TeamCart).GetProperty("AppliedCouponId")?.SetValue(teamCart, couponId);

        // Act
        var result = teamCart.RemoveCoupon(DefaultHostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
    }

    #endregion
}
