using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

/// <summary>
/// Tests for Order aggregate payment-related functionality.
/// </summary>
[TestFixture]
public class OrderPaymentTests : OrderTestHelpers
{
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
        result.ShouldBeSuccessful();
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
        result.ShouldBeSuccessful();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaid));
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
        result.ShouldBeFailure(OrderErrors.PaymentNotFound.Code);
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion
}
