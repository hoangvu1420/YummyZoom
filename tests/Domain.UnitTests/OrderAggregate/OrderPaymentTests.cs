using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

/// <summary>
/// Tests for Order aggregate payment-related functionality.
/// </summary>
[TestFixture]
public class OrderPaymentTests : OrderTestHelpers
{
    #region Two-Phase Payment Flow Tests

    [Test]
    public void ConfirmPayment_WhenStatusIsPendingPayment_ShouldSucceed()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var initialEventCount = order.DomainEvents.Count;
        var timestamp = DateTime.UtcNow;

        // Act
        var result = order.ConfirmPayment(timestamp);

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Placed);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        
        // Verify domain event
        order.DomainEvents.Should().HaveCount(initialEventCount + 1);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentSucceeded));
        var orderPaymentSucceededEvent = order.DomainEvents.OfType<OrderPaymentSucceeded>().Single();
        orderPaymentSucceededEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    public void ConfirmPayment_WhenStatusIsNotPendingPayment_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder(); // Order in Placed status
        var initialEventCount = order.DomainEvents.Count;
        var timestamp = DateTime.UtcNow;

        // Act
        var result = order.ConfirmPayment(timestamp);

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidStatusForPaymentConfirmation.Code);
        order.Status.Should().Be(OrderStatus.Placed); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    [Test]
    public void MarkAsPaymentFailed_WhenStatusIsPendingPayment_ShouldSucceed()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var initialEventCount = order.DomainEvents.Count;
        var timestamp = DateTime.UtcNow;

        // Act
        var result = order.MarkAsPaymentFailed(timestamp);

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        
        // Verify domain event
        order.DomainEvents.Should().HaveCount(initialEventCount + 1);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentFailed));
        var orderPaymentFailedEvent = order.DomainEvents.OfType<OrderPaymentFailed>().Single();
        orderPaymentFailedEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    public void MarkAsPaymentFailed_WhenStatusIsNotPendingPayment_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder(); // Order in Placed status
        var initialEventCount = order.DomainEvents.Count;
        var timestamp = DateTime.UtcNow;

        // Act
        var result = order.MarkAsPaymentFailed(timestamp);

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidStatusForPaymentConfirmation.Code);
        order.Status.Should().Be(OrderStatus.Placed); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    [Test]
    public void Create_WithPendingPaymentStatus_ShouldClearExistingTransactions()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // Create payment transactions that would normally be valid
        var paymentTransactions = CreatePaymentTransactionsWithTotal(totalAmount);

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            subtotal,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount,
            totalAmount,
            paymentTransactions,
            null,
            OrderStatus.PendingPayment, // PendingPayment status
            DefaultPaymentIntentId);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.Status.Should().Be(OrderStatus.PendingPayment);
        // Payment transactions should be cleared for pending payment status
        order.PaymentTransactions.Should().BeEmpty();
    }

    #endregion
}
