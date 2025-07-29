using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.UnitTests.CouponAggregate;
using YummyZoom.Domain.UnitTests.TeamCartAggregate;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using System.Reflection;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services.TeamCartConversionServiceTests;

/// <summary>
/// Tests for TeamCart to Order conversion failure scenarios and edge cases
/// </summary>
[TestFixture]
public class TeamCartConversionServiceFailureTests : TeamCartConversionServiceTestsBase
{
    [Test]
    public void ConvertToOrder_WithInvalidTeamCartStatus_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateValidTeamCart(); // Status is 'Open'
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void ConvertToOrder_WithAlreadyConvertedTeamCart_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateConvertedTeamCart();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void ConvertToOrder_WithExpiredTeamCart_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateExpiredTeamCart();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void ConvertToOrder_WithEmptyItemsList_ShouldFailDueToOrderValidation()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateReadyForConversionCartWithNoItems();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.OrderItemRequired);
    }

    [Test]
    public void ConvertToOrder_WithCouponValidationFailure_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Add items to the cart
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        teamCart.AddItem(teamCart.HostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != teamCart.HostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);

        // Lock the cart for payment so we can apply the coupon
        teamCart.LockForPayment(teamCart.HostUserId).ShouldBeSuccessful();

        // Create a coupon that is genuinely expired
        var expiredCoupon = CouponTestHelpers.CreateValidCoupon();
        typeof(Coupon).GetProperty(nameof(Coupon.ValidityEndDate))!
            .SetValue(expiredCoupon, DateTime.UtcNow.AddDays(-1));

        // Apply the expired coupon (this should succeed at the TeamCart level since it doesn't validate expiry)
        var applyCouponResult = teamCart.ApplyCoupon(teamCart.HostUserId, (CouponId)expiredCoupon.Id);
        applyCouponResult.ShouldBeSuccessful();

        // Complete payments to transition to ReadyToConfirm
        teamCart.RecordSuccessfulOnlinePayment(teamCart.HostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        var guestUserId = teamCart.Members.First(m => m.UserId != teamCart.HostUserId).UserId;
        teamCart.RecordSuccessfulOnlinePayment(guestUserId, new Money(30.00m, Currencies.Default), "txn_guest_456");

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            expiredCoupon,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponExpired);
    }

    [Test]
    public void ConvertToOrder_WithMismatchedCouponId_ShouldSucceedAndIgnoreCoupon()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var appliedCoupon = CouponTestHelpers.CreateValidCoupon();
        teamCart.ApplyCoupon(teamCart.HostUserId, (CouponId)appliedCoupon.Id);

        // A different, valid coupon is passed to the service
        var differentCoupon = CouponTestHelpers.CreateValidCoupon();

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            differentCoupon,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeSuccessful();
        // The discount should be zero because the coupon was ignored due to the ID mismatch.
        result.Value.Order.DiscountAmount.Amount.Should().Be(0);
    }

    [Test]
    public void ConvertToOrder_WithoutMemberPayments_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        var memberPaymentsList = (List<MemberPayment>)typeof(TeamCart)
            .GetField("_memberPayments", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(teamCart)!;

        // Clear all member payments to simulate a scenario where payments weren't properly set up
        memberPaymentsList.Clear();

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotConvertWithoutPayments);
    }

    [Test]
    public void ConvertToOrder_WithNullDeliveryAddress_ShouldFailInOrderCreation()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            null!,
            string.Empty,
            null,
            0,
            new Money(10, Currencies.Default),
            new Money(5, Currencies.Default)
        );

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }
}
