using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
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
using YummyZoom.Domain.UserAggregate.ValueObjects;
using System.Reflection;
using YummyZoom.Domain.OrderAggregate.Errors;

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
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        // Create a coupon that is genuinely expired
        var expiredCoupon = CouponTestHelpers.CreateValidCoupon();
        typeof(Coupon).GetProperty(nameof(Coupon.ValidityEndDate))!
            .SetValue(expiredCoupon, DateTime.UtcNow.AddDays(-1));

        teamCart.ApplyCoupon(teamCart.HostUserId, (CouponId)expiredCoupon.Id);

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
    public void ConvertToOrder_WithFinalPaymentMismatch_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartReadyForConversion();
        var deliveryAddress = DeliveryAddress.Create("123 Main St", "Anytown", "Anystate", "12345", "USA").Value;

        var member1 = teamCart.Members[0];

        var memberPaymentsList = (List<MemberPayment>)typeof(TeamCart)
            .GetField("_memberPayments", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(teamCart)!;

        // Setup payments that will cause a rounding error when adjusted
        memberPaymentsList.Clear();
        memberPaymentsList.Add(
            MemberPayment.Create(member1.UserId, new Money(33.33m, "USD"), PaymentMethod.Online).Value
        );
        memberPaymentsList.Add(
            MemberPayment.Create(UserId.CreateUnique(), new Money(33.33m, "USD"), PaymentMethod.Online).Value
        );
        memberPaymentsList.Add(
            MemberPayment.Create(UserId.CreateUnique(), new Money(33.33m, "USD"), PaymentMethod.Online).Value
        );

        // Act
        // Use real values that will cause the adjustment factor to create a mismatch
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
        result.Error.Should().Be(TeamCartErrors.FinalPaymentMismatch);
    }

    [Test]
    public void ConvertToOrder_WithNoPaymentTransactions_ShouldFail()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateValidTeamCart();
        teamCart.LockForPayment(teamCart.HostUserId);
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
        result.Error.Should().Be(OrderErrors.OrderItemRequired);
    }
}
