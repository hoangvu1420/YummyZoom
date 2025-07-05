using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderLifecycleTests : OrderTestHelpers
{
    #region Accept() Method Tests

    [Test]
    public void Accept_WhenOrderIsPlaced_ShouldSucceedAndUpdateStatusAndTime()
    {
        // Arrange
        var order = CreateValidOrder();
        var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);

        // Act
        var result = order.Accept(estimatedDeliveryTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Accepted);
        order.EstimatedDeliveryTime.Should().Be(estimatedDeliveryTime);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().Contain(e => e.GetType() == typeof(OrderAccepted));
        var orderAcceptedEvent = order.DomainEvents.OfType<OrderAccepted>().Single();
        orderAcceptedEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    public void Accept_WhenOrderIsNotPlaced_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Accept(DateTime.UtcNow.AddHours(1)); // Move to Accepted state
        var estimatedDeliveryTime = DateTime.UtcNow.AddHours(2);
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Accept(estimatedDeliveryTime);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidOrderStatusForAccept);
        order.Status.Should().Be(OrderStatus.Accepted); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region MarkAsPreparing() Method Tests

    [Test]
    public void MarkAsPreparing_WhenOrderIsAccepted_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreateAcceptedOrder();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsPreparing();

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Preparing);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderPreparing));
        var orderPreparingEvent = order.DomainEvents.OfType<OrderPreparing>().Single();
        orderPreparingEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    [TestCase(OrderStatus.Placed)]
    [TestCase(OrderStatus.ReadyForDelivery)]
    [TestCase(OrderStatus.Delivered)]
    [TestCase(OrderStatus.Cancelled)]
    [TestCase(OrderStatus.Rejected)]
    public void MarkAsPreparing_WhenOrderIsNotAccepted_ShouldFailWithInvalidStatusError(OrderStatus invalidStatus)
    {
        // Arrange
        Order order;
        switch (invalidStatus)
        {
            case OrderStatus.Placed:
                order = CreateValidOrder();
                break;
            case OrderStatus.ReadyForDelivery:
                order = CreateReadyForDeliveryOrder();
                break;
            case OrderStatus.Delivered:
                order = CreateReadyForDeliveryOrder(); // Transition to Delivered
                order.MarkAsDelivered().ShouldBeSuccessful(); 
                break;
            case OrderStatus.Cancelled:
                order = CreateValidOrder(); // Transition to Cancelled
                order.Cancel().ShouldBeSuccessful(); 
                break;
            case OrderStatus.Rejected:
                order = CreateValidOrder(); // Transition to Rejected
                order.Reject().ShouldBeSuccessful(); 
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus), invalidStatus, null);
        }
        
        var initialStatus = order.Status;
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsPreparing();

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidOrderStatusForPreparing.Code);
        order.Status.Should().Be(initialStatus); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region MarkAsReadyForDelivery() Method Tests

    [Test]
    public void MarkAsReadyForDelivery_WhenOrderIsPreparing_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreatePreparingOrder();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsReadyForDelivery();

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderReadyForDelivery));
        var orderReadyForDeliveryEvent = order.DomainEvents.OfType<OrderReadyForDelivery>().Single();
        orderReadyForDeliveryEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    [TestCase(OrderStatus.Placed)]
    [TestCase(OrderStatus.Accepted)]
    [TestCase(OrderStatus.Delivered)]
    [TestCase(OrderStatus.Cancelled)]
    [TestCase(OrderStatus.Rejected)]
    public void MarkAsReadyForDelivery_WhenOrderIsNotPreparing_ShouldFailWithInvalidStatusError(OrderStatus invalidStatus)
    {
        // Arrange
        Order order;
        switch (invalidStatus)
        {
            case OrderStatus.Placed:
                order = CreateValidOrder();
                break;
            case OrderStatus.Accepted:
                order = CreateAcceptedOrder();
                break;
            case OrderStatus.Delivered:
                order = CreateReadyForDeliveryOrder(); // Transition to Delivered
                order.MarkAsDelivered().ShouldBeSuccessful(); 
                break;
            case OrderStatus.Cancelled:
                order = CreateValidOrder(); // Transition to Cancelled
                order.Cancel().ShouldBeSuccessful(); 
                break;
            case OrderStatus.Rejected:
                order = CreateValidOrder(); // Transition to Rejected
                order.Reject().ShouldBeSuccessful(); 
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus), invalidStatus, null);
        }
        
        var initialStatus = order.Status;
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsReadyForDelivery();

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidOrderStatusForReadyForDelivery.Code);
        order.Status.Should().Be(initialStatus); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region MarkAsDelivered() Method Tests

    [Test]
    public void MarkAsDelivered_WhenOrderIsReadyForDelivery_ShouldSucceedAndUpdateStatusAndTime()
    {
        // Arrange
        var order = CreateReadyForDeliveryOrder();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsDelivered();

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.ActualDeliveryTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().HaveCount(initialEventCount + 1); 
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderDelivered));
        var orderDeliveredEvent = order.DomainEvents.OfType<OrderDelivered>().Single();
        orderDeliveredEvent.OrderId.Should().Be(order.Id);
        orderDeliveredEvent.DeliveredAt.Should().Be(order.ActualDeliveryTime!.Value);
    }

    [Test]
    [TestCase(OrderStatus.Placed)]
    [TestCase(OrderStatus.Accepted)]
    [TestCase(OrderStatus.Preparing)]
    [TestCase(OrderStatus.Cancelled)]
    [TestCase(OrderStatus.Rejected)]
    public void MarkAsDelivered_WhenOrderIsNotReadyForDelivery_ShouldFailWithInvalidStatusError(OrderStatus invalidStatus)
    {
        // Arrange
        Order order;
        switch (invalidStatus)
        {
            case OrderStatus.Placed:
                order = CreateValidOrder();
                break;
            case OrderStatus.Accepted:
                order = CreateAcceptedOrder();
                break;
            case OrderStatus.Preparing:
                order = CreatePreparingOrder();
                break;
            case OrderStatus.Cancelled:
                order = CreateValidOrder(); // Transition to Cancelled
                order.Cancel().ShouldBeSuccessful(); 
                break;
            case OrderStatus.Rejected:
                order = CreateValidOrder(); // Transition to Rejected
                order.Reject().ShouldBeSuccessful(); 
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus), invalidStatus, null);
        }
        
        var initialStatus = order.Status;
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.MarkAsDelivered();

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidOrderStatusForDelivered.Code);
        order.Status.Should().Be(initialStatus); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region Reject() Method Tests

    [Test]
    public void Reject_WhenOrderIsPlaced_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Reject();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Rejected);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().Contain(e => e.GetType() == typeof(OrderRejected));
        var orderRejectedEvent = order.DomainEvents.OfType<OrderRejected>().Single();
        orderRejectedEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    public void Reject_WhenOrderIsNotPlaced_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Accept(DateTime.UtcNow.AddHours(1)); // Move to Accepted state
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Reject();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusForReject);
        order.Status.Should().Be(OrderStatus.Accepted); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion

    #region Cancel() Method Tests

    [Test]
    public void Cancel_WhenOrderIsPlaced_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = order.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().Contain(e => e.GetType() == typeof(OrderCancelled));
        var orderCancelledEvent = order.DomainEvents.OfType<OrderCancelled>().Single();
        orderCancelledEvent.OrderId.Should().Be(order.Id);
    }

    [Test]
    public void Cancel_WhenOrderIsAccepted_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Accept(DateTime.UtcNow.AddHours(1));

        // Act
        var result = order.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public void Cancel_WhenOrderIsPreparing_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreatePreparingOrder();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Cancel();

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().HaveCount(initialEventCount + 1);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCancelled));
    }

    [Test]
    public void Cancel_WhenOrderIsReadyForDelivery_ShouldSucceedAndUpdateStatus()
    {
        // Arrange
        var order = CreateReadyForDeliveryOrder();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Cancel();

        // Assert
        result.ShouldBeSuccessful();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event
        order.DomainEvents.Should().HaveCount(initialEventCount + 1);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCancelled));
    }

    [Test]
    [TestCase(OrderStatus.Delivered)]
    [TestCase(OrderStatus.Rejected)]
    public void Cancel_WhenOrderIsInvalidStatus_ShouldFailWithInvalidStatusError(OrderStatus invalidStatus)
    {
        // Arrange
        Order order;
        switch (invalidStatus)
        {
            case OrderStatus.Delivered:
                order = CreateReadyForDeliveryOrder();
                order.MarkAsDelivered().ShouldBeSuccessful();
                break;
            case OrderStatus.Rejected:
                order = CreateValidOrder();
                order.Reject().ShouldBeSuccessful();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus), invalidStatus, null);
        }
        
        var initialStatus = order.Status;
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Cancel();

        // Assert
        result.ShouldBeFailure(OrderErrors.InvalidOrderStatusForCancel.Code);
        order.Status.Should().Be(initialStatus); // Status unchanged
        order.DomainEvents.Should().HaveCount(initialEventCount); // No new events
    }

    #endregion
}
