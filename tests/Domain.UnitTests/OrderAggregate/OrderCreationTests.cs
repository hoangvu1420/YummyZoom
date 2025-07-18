using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderCreationTests : OrderTestHelpers
{
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
        order.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public void Create_WithCoupon_ShouldSucceedAndStoreCoupon()
    {
        // Arrange
        var couponId = CouponId.CreateUnique();

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            appliedCouponId: couponId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.AppliedCouponId.Should().Be(couponId);
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
}
