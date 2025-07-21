using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

/// <summary>
/// Tests for the complete payment flow in the Order aggregate.
/// </summary>
[TestFixture]
public class OrderPaymentFlowTests : OrderTestHelpers
{
    #region Order Creation with Payment Flow

    [Test]
    public void CreateOrder_WithPendingPaymentStatus_ShouldSucceed()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        var paymentIntentId = "pi_test_12345";

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
            new List<PaymentTransaction>(), // Empty payment transactions
            null,
            OrderStatus.PendingPayment,
            paymentIntentId);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.PaymentIntentId.Should().Be(paymentIntentId);
        order.PaymentTransactions.Should().BeEmpty();
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCreated));
    }

    [Test]
    public void Create_WithPaymentIntentId_ShouldStoreId()
    {
        // Arrange
        var paymentIntentId = "pi_test_12345";
        
        // Act
        var order = CreateOrderWithPaymentIntent(paymentIntentId);
        
        // Assert
        order.PaymentIntentId.Should().Be(paymentIntentId);
    }

    [Test]
    public void Create_WithPendingPaymentStatus_RequiresPaymentIntentId()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            new List<PaymentTransaction>(),
            null,
            OrderStatus.PendingPayment,
            null); // No payment intent ID

        // Assert
        // The order creation should fail because payment intent ID is required for PendingPayment status
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.PaymentIntentIdRequired);
    }

    #endregion

    #region Complete Payment Flow Tests

    [Test]
    public void PendingPaymentOrder_ConfirmPayment_ShouldTransitionToPlaced()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        // Act
        var result = order.ConfirmPayment(timestamp);
        
        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Placed);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentSucceeded));
    }

    [Test]
    public void PendingPaymentOrder_MarkAsPaymentFailed_ShouldTransitionToPaymentFailed()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        // Act
        var result = order.MarkAsPaymentFailed(timestamp);
        
        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentFailed));
    }

    [Test]
    public void PaymentFailedOrder_CannotTransitionToOtherStates()
    {
        // Arrange
        var order = CreatePaymentFailedOrder();
        var timestamp = DateTime.UtcNow;
        var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);
        
        // Act & Assert - Try all possible transitions
        
        // Try to confirm payment
        var confirmResult = order.ConfirmPayment(timestamp);
        confirmResult.ShouldBeFailure(OrderErrors.InvalidStatusForPaymentConfirmation.Code);
        
        // Try to accept
        var acceptResult = order.Accept(estimatedDeliveryTime, timestamp);
        acceptResult.ShouldBeFailure(OrderErrors.InvalidOrderStatusForAccept.Code);
        
        // Try to reject
        var rejectResult = order.Reject(timestamp);
        rejectResult.ShouldBeFailure(OrderErrors.InvalidStatusForReject.Code);
        
        // Try to cancel
        var cancelResult = order.Cancel(timestamp);
        cancelResult.ShouldBeFailure(OrderErrors.InvalidOrderStatusForCancel.Code);
        
        // Try to mark as preparing
        var preparingResult = order.MarkAsPreparing(timestamp);
        preparingResult.ShouldBeFailure(OrderErrors.InvalidOrderStatusForPreparing.Code);
        
        // Try to mark as ready for delivery
        var readyResult = order.MarkAsReadyForDelivery(timestamp);
        readyResult.ShouldBeFailure(OrderErrors.InvalidOrderStatusForReadyForDelivery.Code);
        
        // Try to mark as delivered
        var deliveredResult = order.MarkAsDelivered(timestamp);
        deliveredResult.ShouldBeFailure(OrderErrors.InvalidOrderStatusForDelivered.Code);
        
        // Verify status hasn't changed
        order.Status.Should().Be(OrderStatus.PaymentFailed);
    }

    [Test]
    public void CompletePaymentFlow_FromPendingToPlacedToDelivered()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        // Act - Step 1: Confirm payment
        var confirmResult = order.ConfirmPayment(timestamp);
        confirmResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Placed);
        
        // Act - Step 2: Accept order
        var estimatedDeliveryTime = timestamp.AddHours(1);
        var acceptResult = order.Accept(estimatedDeliveryTime, timestamp.AddMinutes(5));
        acceptResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Accepted);
        
        // Act - Step 3: Mark as preparing
        var preparingResult = order.MarkAsPreparing(timestamp.AddMinutes(10));
        preparingResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Preparing);
        
        // Act - Step 4: Mark as ready for delivery
        var readyResult = order.MarkAsReadyForDelivery(timestamp.AddMinutes(30));
        readyResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
        
        // Act - Step 5: Mark as delivered
        var deliveredResult = order.MarkAsDelivered(timestamp.AddMinutes(45));
        deliveredResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Delivered);
        
        // Assert - Verify final state
        order.Status.Should().Be(OrderStatus.Delivered);
        order.ActualDeliveryTime.Should().Be(timestamp.AddMinutes(45));
        
        // Verify domain events
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentSucceeded));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderAccepted));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPreparing));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderReadyForDelivery));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderDelivered));
    }

    [Test]
    public void CompletePaymentFlow_FromPendingToFailedIsTerminal()
    {
        // Arrange
        var order = CreatePendingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        // Act - Mark payment as failed
        var failedResult = order.MarkAsPaymentFailed(timestamp);
        failedResult.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.PaymentFailed);
        
        // Assert - Verify that no further transitions are possible
        var confirmResult = order.ConfirmPayment(timestamp.AddMinutes(5));
        confirmResult.ShouldBeFailure();
        
        var acceptResult = order.Accept(timestamp.AddHours(1), timestamp.AddMinutes(5));
        acceptResult.ShouldBeFailure();
        
        // Verify final state
        order.Status.Should().Be(OrderStatus.PaymentFailed);
        
        // Verify domain events
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentFailed));
    }

    #endregion
}
