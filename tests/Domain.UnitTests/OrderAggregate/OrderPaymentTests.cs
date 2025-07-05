using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

/// <summary>
/// Tests for Order aggregate payment-related functionality.
/// </summary>
[TestFixture]
public class OrderPaymentTests
{
    private static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly DeliveryAddress DefaultDeliveryAddress = CreateDefaultDeliveryAddress();
    private static readonly List<OrderItem> DefaultOrderItems = CreateDefaultOrderItems();
    private const string DefaultSpecialInstructions = "No special instructions";
    private static readonly Money DefaultDiscountAmount = Money.Zero(Currencies.Default);
    private static readonly Money DefaultDeliveryFee = new Money(5.00m, Currencies.Default);
    private static readonly Money DefaultTipAmount = new Money(2.00m, Currencies.Default);
    private static readonly Money DefaultTaxAmount = new Money(1.50m, Currencies.Default);

    #region Payment Methods Tests

    [Test]
    public void AddPaymentAttempt_ShouldSucceedAndAddPaymentToCollection()
    {
        // Arrange
        var order = CreateValidOrder();
        var payment = CreateValidPaymentTransaction();

        // Act
        var result = order.AddPaymentAttempt(payment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.PaymentTransactions.Should().ContainSingle();
        order.PaymentTransactions.Should().Contain(payment);
    }

    [Test]
    public void MarkAsPaid_WithValidPaymentId_ShouldSucceedAndMarkPaymentAsSucceeded()
    {
        // Arrange
        var order = CreateValidOrder();
        var payment = CreateValidPaymentTransaction();
        order.AddPaymentAttempt(payment);

        // Act
        var result = order.MarkAsPaid(payment.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        
        // Verify domain event
        order.DomainEvents.Should().Contain(e => e.GetType() == typeof(OrderPaid));
        var orderPaidEvent = order.DomainEvents.OfType<OrderPaid>().Single();
        orderPaidEvent.OrderId.Should().Be(order.Id);
        orderPaidEvent.PaymentTransactionId.Should().Be(payment.Id);
    }

    [Test]
    public void MarkAsPaid_WithInvalidPaymentId_ShouldFailWithPaymentNotFoundError()
    {
        // Arrange
        var order = CreateValidOrder();
        var invalidPaymentId = PaymentTransactionId.CreateUnique();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsPaid(invalidPaymentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.PaymentNotFound);
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region Helper Methods

    private static Order CreateValidOrder()
    {
        return Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount).Value;
    }

    private static DeliveryAddress CreateDefaultDeliveryAddress()
    {
        return DeliveryAddress.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "USA").Value;
    }

    private static List<OrderItem> CreateDefaultOrderItems()
    {
        var orderItem = OrderItem.Create(
            MenuCategoryId.CreateUnique(),
            MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;

        return new List<OrderItem> { orderItem };
    }

    private static PaymentTransaction CreateValidPaymentTransaction()
    {
        return PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(25.50m, Currencies.Default),
            DateTime.UtcNow).Value;
    }

    #endregion
}
