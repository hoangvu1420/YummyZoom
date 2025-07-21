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
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // Create payment transactions that match the total amount
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
            null);

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
        order.PaymentIntentId.Should().BeNull();
        
        // Verify calculated amounts
        order.Subtotal.Should().Be(subtotal);
        order.TotalAmount.Should().Be(totalAmount);
        
        // Verify payment transactions
        order.PaymentTransactions.Should().HaveCount(paymentTransactions.Count);
        order.PaymentTransactions.Should().BeEquivalentTo(paymentTransactions);
        
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
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // For minimal inputs, use zero for all monetary values except subtotal
        var discountAmount = Money.Zero(Currencies.Default);
        var deliveryFee = Money.Zero(Currencies.Default);
        var tipAmount = Money.Zero(Currencies.Default);
        var taxAmount = Money.Zero(Currencies.Default);
        
        // Total amount equals subtotal when all other values are zero
        var totalAmount = subtotal;
        
        // Create payment transactions that match the total amount
        var paymentTransactions = CreatePaymentTransactionsWithTotal(totalAmount);
        
        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            totalAmount,
            paymentTransactions,
            null);

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
        
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // Create payment transactions that match the total amount
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
            couponId);

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
        
        // Use zero for all monetary values since we expect failure before validation
        var subtotal = Money.Zero(Currencies.Default);
        var totalAmount = Money.Zero(Currencies.Default);
        var paymentTransactions = new List<PaymentTransaction>();

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            emptyOrderItems,
            DefaultSpecialInstructions,
            subtotal,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount,
            totalAmount,
            paymentTransactions,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderItemRequired);
    }

    [Test]
    public void Create_WithNegativeTotalAmount_ShouldFailWithNegativeTotalAmountError()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Create a large discount that will make the total negative
        var largeDiscount = new Money(subtotal.Amount + 50m, Currencies.Default);
        
        // Calculate total amount (will be negative)
        var totalAmount = subtotal - largeDiscount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // Create payment transactions (won't be validated due to negative total)
        var paymentTransactions = new List<PaymentTransaction>();

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            subtotal,
            largeDiscount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount,
            totalAmount,
            paymentTransactions,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NegativeTotalAmount);
    }
    
    [Test]
    public void Create_WithFinancialMismatch_ShouldFailWithFinancialMismatchError()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate correct total amount
        var correctTotalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // Create incorrect total amount (add $10)
        var incorrectTotalAmount = new Money(correctTotalAmount.Amount + 10m, Currencies.Default);
        
        // Create payment transactions that match the incorrect total
        var paymentTransactions = CreatePaymentTransactionsWithTotal(incorrectTotalAmount);

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
            incorrectTotalAmount, // Incorrect total that doesn't match calculation
            paymentTransactions,
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.FinancialMismatch);
    }
    
    [Test]
    public void Create_WithSpecifiedInitialStatus_ShouldSetCorrectStatus()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // For PendingPayment status, we don't need payment transactions
        var initialStatus = OrderStatus.PendingPayment;

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
            new List<PaymentTransaction>(), // Empty for pending payment
            null,
            initialStatus,
            DefaultPaymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.Status.Should().Be(initialStatus);
    }
    
    [Test]
    public void Create_WithPaymentIntentId_ShouldStorePaymentIntentId()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // For PendingPayment status with payment intent
        var initialStatus = OrderStatus.PendingPayment;
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
            new List<PaymentTransaction>(), // Empty for pending payment
            null,
            initialStatus,
            paymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.PaymentIntentId.Should().Be(paymentIntentId);
    }
    
    [Test]
    public void Create_WithPendingPaymentStatus_ShouldNotRequirePaymentTransactions()
    {
        // Arrange
        // Calculate subtotal from order items
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
        // Calculate total amount
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        
        // For PendingPayment status, we don't need payment transactions
        var initialStatus = OrderStatus.PendingPayment;
        
        // Create some payment transactions that would normally cause a mismatch
        var incorrectPaymentTransactions = CreatePaymentTransactionsWithMismatchedTotal(totalAmount);

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
            incorrectPaymentTransactions, // These would normally cause a mismatch
            null,
            initialStatus,
            DefaultPaymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;
        order.Status.Should().Be(initialStatus);
        // Payment transactions should be cleared for pending payment status
        order.PaymentTransactions.Should().BeEmpty();
    }
}
