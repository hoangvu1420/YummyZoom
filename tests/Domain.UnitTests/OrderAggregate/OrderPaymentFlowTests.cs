using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using static YummyZoom.Domain.UnitTests.OrderAggregate.OrderTestHelpers;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderPaymentFlowTests
{
    [Test]
    public void AwaitingPaymentOrder_RecordPaymentSuccess_ShouldTransitionToPlaced()
    {
        var order = CreateAwaitingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        var result = order.RecordPaymentSuccess(DefaultPaymentGatewayReferenceId, timestamp);
        
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Placed);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentSucceeded));
    }

    [Test]
    public void AwaitingPaymentOrder_RecordPaymentFailure_ShouldTransitionToCancelled()
    {
        var order = CreateAwaitingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        var result = order.RecordPaymentFailure(DefaultPaymentGatewayReferenceId, timestamp);
        
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.LastUpdateTimestamp.Should().Be(timestamp);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentFailed));
    }

    [Test]
    public void CancelledOrder_CannotTransitionToOtherStates()
    {
        var order = CreateAwaitingPaymentOrder();
        order.RecordPaymentFailure(DefaultPaymentGatewayReferenceId).ShouldBeSuccessful();
        var timestamp = DateTime.UtcNow;
        var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);
        
        order.RecordPaymentSuccess(DefaultPaymentGatewayReferenceId, timestamp).ShouldBeFailure(OrderErrors.InvalidStatusForPaymentConfirmation.Code);
        order.Accept(estimatedDeliveryTime, timestamp).ShouldBeFailure(OrderErrors.InvalidOrderStatusForAccept.Code);
        order.Reject(timestamp).ShouldBeFailure(OrderErrors.InvalidStatusForReject.Code);
        order.Cancel(timestamp).ShouldBeFailure(OrderErrors.InvalidOrderStatusForCancel.Code);
        order.MarkAsPreparing(timestamp).ShouldBeFailure(OrderErrors.InvalidOrderStatusForPreparing.Code);
        order.MarkAsReadyForDelivery(timestamp).ShouldBeFailure(OrderErrors.InvalidOrderStatusForReadyForDelivery.Code);
        order.MarkAsDelivered(timestamp).ShouldBeFailure(OrderErrors.InvalidOrderStatusForDelivered.Code);
        
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public void CompletePaymentFlow_FromAwaitingPaymentToPlacedToDelivered()
    {
        var order = CreateAwaitingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        order.RecordPaymentSuccess(DefaultPaymentGatewayReferenceId, timestamp).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Placed);
        
        var estimatedDeliveryTime = timestamp.AddHours(1);
        order.Accept(estimatedDeliveryTime, timestamp.AddMinutes(5)).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Accepted);
        
        order.MarkAsPreparing(timestamp.AddMinutes(10)).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Preparing);
        
        order.MarkAsReadyForDelivery(timestamp.AddMinutes(30)).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
        
        order.MarkAsDelivered(timestamp.AddMinutes(45)).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Delivered);
        
        order.ActualDeliveryTime.Should().Be(timestamp.AddMinutes(45));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentSucceeded));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderAccepted));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPreparing));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderReadyForDelivery));
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderDelivered));
    }

    [Test]
    public void CompletePaymentFlow_FromAwaitingPaymentToCancelledIsTerminal()
    {
        var order = CreateAwaitingPaymentOrder();
        var timestamp = DateTime.UtcNow;
        
        order.RecordPaymentFailure(DefaultPaymentGatewayReferenceId, timestamp).ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Cancelled);
        
        order.RecordPaymentSuccess(DefaultPaymentGatewayReferenceId, timestamp.AddMinutes(5)).ShouldBeFailure();
        order.Accept(timestamp.AddHours(1), timestamp.AddMinutes(5)).ShouldBeFailure();
        
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPaymentFailed));
    }

    [Test]
    public void RecordPaymentSuccess_WithInvalidGatewayReferenceId_ShouldFail()
    {
        var order = CreateAwaitingPaymentOrder();
        var result = order.RecordPaymentSuccess("invalid-id");

        result.ShouldBeFailure(OrderErrors.PaymentTransactionNotFound.Code);
    }

    [Test]
    public void RecordPaymentFailure_WithInvalidGatewayReferenceId_ShouldFail()
    {
        var order = CreateAwaitingPaymentOrder();
        var result = order.RecordPaymentFailure("invalid-id");

        result.ShouldBeFailure(OrderErrors.PaymentTransactionNotFound.Code);
    }
}
