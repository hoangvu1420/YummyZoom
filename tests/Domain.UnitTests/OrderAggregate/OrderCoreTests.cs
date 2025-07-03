using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
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
/// Tests for core Order aggregate functionality including creation and lifecycle management.
/// </summary>
[TestFixture]
public class OrderCoreTests
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

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeOrderCorrectly()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        
        order.Id.Value.Should().NotBe(Guid.Empty);
        order.CustomerId.Should().Be(DefaultCustomerId);
        order.RestaurantId.Should().Be(DefaultRestaurantId);
        order.DeliveryAddress.Should().Be(DefaultDeliveryAddress);
        order.SpecialInstructions.Should().Be(DefaultSpecialInstructions);
        order.DiscountAmount.Should().Be(DefaultDiscountAmount);
        order.DeliveryFee.Should().Be(DefaultDeliveryFee);
        order.TipAmount.Should().Be(DefaultTipAmount);
        order.TaxAmount.Should().Be(DefaultTaxAmount);
        order.OrderItems.Should().HaveCount(DefaultOrderItems.Count);
        order.OrderItems.Should().BeEquivalentTo(DefaultOrderItems);
        order.Status.Should().Be(OrderStatus.Placed);
        order.PlacementTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.EstimatedDeliveryTime.Should().BeNull();
        
        // Verify calculated amounts
        var expectedSubtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        order.Subtotal.Should().Be(expectedSubtotal);
        
        var expectedTotal = new Money(expectedSubtotal.Amount - DefaultDiscountAmount.Amount + 
                                    DefaultTaxAmount.Amount + DefaultDeliveryFee.Amount + DefaultTipAmount.Amount, Currencies.Default);
        order.TotalAmount.Should().Be(expectedTotal);
        
        // Verify domain event
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCreated));
        var orderCreatedEvent = order.DomainEvents.OfType<OrderCreated>().Single();
        orderCreatedEvent.OrderId.Should().Be(order.Id);
        orderCreatedEvent.CustomerId.Should().Be(DefaultCustomerId);
        orderCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
        orderCreatedEvent.TotalAmount.Should().Be(order.TotalAmount);
    }

    [Test]
    public void Create_WithMinimalInputs_ShouldSucceedWithDefaults()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        
        order.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
        order.DeliveryFee.Should().Be(Money.Zero(Currencies.Default));
        order.TipAmount.Should().Be(Money.Zero(Currencies.Default));
        order.TaxAmount.Should().Be(Money.Zero(Currencies.Default));
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void Create_WithCoupons_ShouldSucceedAndStoreCoupons()
    {
        // Arrange
        var couponIds = new List<CouponId> { CouponId.CreateUnique(), CouponId.CreateUnique() };

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            appliedCouponIds: couponIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.AppliedCouponIds.Should().HaveCount(2);
        order.AppliedCouponIds.Should().BeEquivalentTo(couponIds);
    }

    [Test]
    public void Create_WithEmptyOrderItems_ShouldFailWithOrderItemRequiredError()
    {
        // Arrange
        var emptyOrderItems = new List<OrderItem>();

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            emptyOrderItems,
            DefaultSpecialInstructions);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemRequired);
    }

    [Test]
    public void Create_WithNegativeTotalAmount_ShouldFailWithNegativeTotalAmountError()
    {
        // Arrange
        var largeDiscount = new Money(1000m, Currencies.Default); // Much larger than the order total

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            discountAmount: largeDiscount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NegativeTotalAmount);
    }

    #endregion

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
    public void Cancel_WhenOrderIsDelivered_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var order = CreateValidOrder();
        // Move order through states to Delivered (we'll need additional methods for this in future)
        // For now, we'll test with an order that's been rejected (invalid state for cancel)
        order.Reject();
        var initialEventCount = order.DomainEvents.Count;

        // Act
        var result = order.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidOrderStatusForCancel);
        order.Status.Should().Be(OrderStatus.Rejected); // Status unchanged
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

    #endregion
}
