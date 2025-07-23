using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.UnitTests.CouponAggregate;
using YummyZoom.Domain.UnitTests.TeamCartAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services.TeamCartConversionServiceTests;

/// <summary>
/// Tests for successful TeamCart to Order conversion scenarios
/// </summary>
[TestFixture]
public class TeamCartConversionServiceSuccessTests : TeamCartConversionServiceTestsBase
{
    [Test]
    public void ConvertToOrder_WithValidCODPayment_ShouldSucceed()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversionWithCODPayment();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected total = 55 (subtotal) + 10 (delivery) + 5 (tax) = 70
        var expectedTotal = new Money(70m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", null, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify COD payment transactions
        var codTransactions = order.PaymentTransactions
            .Where(t => t.PaymentMethodType == PaymentMethodType.CreditCard)
            .ToList();
        codTransactions.Should().BeEmpty();
        order.PaymentTransactions
            .All(pt => pt.PaymentMethodType == PaymentMethodType.CashOnDelivery)
            .Should().BeTrue();
    }

    [Test]
    public void ConvertToOrder_WithValidOnlinePayment_ShouldSucceed()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected total = 55 (subtotal) + 10 (delivery) + 5 (tax) = 70
        var expectedTotal = new Money(70m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", null, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify online payment transactions
        order.PaymentTransactions
            .All(pt => pt.PaymentMethodType == PaymentMethodType.CreditCard)
            .Should().BeTrue();
    }

    [Test]
    public void ConvertToOrder_WithMixedPayments_ShouldSucceed()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        teamCart.AddItem(
            teamCart.HostUserId,
            MenuItemId.CreateUnique(),
            MenuCategoryId.CreateUnique(),
            "Host Item",
            new Money(50.00m, Currencies.Default),
            1
        );
        teamCart.AddItem(
            teamCart.Members.First(m => m.Role == MemberRole.Guest).UserId,
            MenuItemId.CreateUnique(),
            MenuCategoryId.CreateUnique(),
            "Guest Item",
            new Money(40.00m, Currencies.Default),
            1
        );

        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var hostId = teamCart.HostUserId;
        var guestId = teamCart.Members.First(m => m.Role == MemberRole.Guest).UserId;

        teamCart.LockForPayment(hostId);
        teamCart.RecordSuccessfulOnlinePayment(hostId, new Money(50m, Currencies.Default), "txn_123");
        teamCart.CommitToCashOnDelivery(guestId, new Money(40m, Currencies.Default));

        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected total = 90 (subtotal) + 10 (delivery) + 5 (tax) = 105
        var expectedTotal = new Money(105m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", null, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify both payment transactions exist
        order.PaymentTransactions.Should().HaveCount(2);
        order.PaymentTransactions.Should().Contain(t => t.PaymentMethodType == PaymentMethodType.CreditCard);
        order.PaymentTransactions.Should().Contain(t => t.PaymentMethodType == PaymentMethodType.CashOnDelivery);
    }

    [Test]
    public void ConvertToOrder_WithValidCoupon_ShouldApplyDiscountAndSucceed()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        // Create a 10% discount coupon
        var coupon = CouponTestHelpers.CreatePercentageCoupon(10);
        teamCart.ApplyCoupon(teamCart.HostUserId, (CouponId)coupon.Id);

        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected discount = 10% of 55 (subtotal) = 5.50
        var expectedDiscount = new Money(5.50m, Currencies.Default);
        // Expected total = 55 (subtotal) - 5.50 (discount) + 10 (delivery) + 5 (tax) = 64.50
        var expectedTotal = new Money(64.50m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", coupon, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify coupon was applied
        order.AppliedCouponId.Should().NotBeNull();
        order.AppliedCouponId.Should().Be(coupon.Id);
        order.DiscountAmount.Should().Be(expectedDiscount);
    }

    [Test]
    public void ConvertToOrder_WithTip_ShouldIncludeTipInTotal()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var tipAmount = new Money(15m, Currencies.Default);
        teamCart.LockForPayment(teamCart.HostUserId); // Need to lock before applying tip
        teamCart.ApplyTip(teamCart.HostUserId, tipAmount);

        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected total = 55 (subtotal) + 15 (tip) + 10 (delivery) + 5 (tax) = 85
        var expectedTotal = new Money(85m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", null, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify tip was included
        order.TipAmount.Should().Be(tipAmount);
    }

    [Test]
    public void ConvertToOrder_WithCouponAndTip_ShouldApplyBothCorrectly()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var coupon = CouponTestHelpers.CreatePercentageCoupon(20); // 20% discount
        var tipAmount = new Money(10m, Currencies.Default);

        teamCart.LockForPayment(teamCart.HostUserId);
        teamCart.ApplyCoupon(teamCart.HostUserId, (CouponId)coupon.Id);
        teamCart.ApplyTip(teamCart.HostUserId, tipAmount);

        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);

        // Expected discount = 20% of 55 (subtotal) = 11
        var expectedDiscount = new Money(11m, Currencies.Default);
        // Expected total = 55 (subtotal) - 11 (discount) + 10 (tip) + 10 (delivery) + 5 (tax) = 69
        var expectedTotal = new Money(69m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", coupon, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;

        VerifyOrderProperties(order, updatedTeamCart, expectedTotal);
        VerifyPaymentTransactions(order, updatedTeamCart);
        VerifyTeamCartState(updatedTeamCart);

        // Verify both coupon and tip
        order.AppliedCouponId.Should().NotBeNull();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.TipAmount.Should().Be(tipAmount);
    }

    [Test]
    public void ConvertToOrder_WithDeliveryAddress_ShouldSetCorrectAddress()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;
        var deliveryFee = new Money(10, Currencies.Default);
        var taxAmount = new Money(5, Currencies.Default);
        var expectedTotal = new Money(70m, Currencies.Default);

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart, deliveryAddress, "", null, 0, deliveryFee, taxAmount);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;

        VerifyOrderProperties(order, teamCart, expectedTotal);

        // Verify delivery address
        order.DeliveryAddress.Should().NotBeNull();
        order.DeliveryAddress.Should().Be(deliveryAddress);
    }
}
