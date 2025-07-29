using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using static YummyZoom.Domain.UnitTests.OrderAggregate.OrderTestHelpers;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderCreationTests
{
    [Test]
    public void Create_WithCashOnDelivery_ShouldSucceedAndInitializeOrderAsPlaced()
    {
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            PaymentMethodType.CashOnDelivery,
            null);

        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.Status.Should().Be(OrderStatus.Placed);
        order.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().Status.Should().Be(PaymentStatus.Succeeded);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCreated));
    }

    [Test]
    public void Create_WithOnlinePayment_ShouldSucceedAndInitializeOrderAsAwaitingPayment()
    {
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            PaymentMethodType.CreditCard,
            null,
            DefaultPaymentGatewayReferenceId);

        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.Status.Should().Be(OrderStatus.AwaitingPayment);
        order.PaymentTransactions.Should().HaveCount(1);
        var transaction = order.PaymentTransactions.First();
        transaction.Status.Should().Be(PaymentStatus.Pending);
        transaction.PaymentGatewayReferenceId.Should().Be(DefaultPaymentGatewayReferenceId);
        order.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(OrderCreated));
    }

    [Test]
    public void Create_WithOnlinePayment_AndMissingGatewayReferenceId_ShouldFail()
    {
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            PaymentMethodType.CreditCard,
            null,
            null); // Missing gateway reference ID

        result.ShouldBeFailure(OrderErrors.PaymentGatewayReferenceIdRequired.Code);
    }

    [Test]
    public void Create_WithCoupon_ShouldSucceedAndStoreCoupon()
    {
        var couponId = CouponId.CreateUnique();
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var totalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            PaymentMethodType.CashOnDelivery,
            couponId);

        result.ShouldBeSuccessful();
        var order = result.Value;
        order.AppliedCouponId.Should().Be(couponId);
    }

    [Test]
    public void Create_WithEmptyOrderItems_ShouldFailWithOrderItemRequiredError()
    {
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            new List<Domain.OrderAggregate.Entities.OrderItem>(),
            DefaultSpecialInstructions,
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            PaymentMethodType.CashOnDelivery,
            null);

        result.ShouldBeFailure(OrderErrors.OrderItemRequired.Code);
    }

    [Test]
    public void Create_WithNegativeTotalAmount_ShouldFailWithNegativeTotalAmountError()
    {
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var largeDiscount = new Money(subtotal.Amount + 50m, Currencies.Default);
        var totalAmount = subtotal - largeDiscount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;

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
            PaymentMethodType.CashOnDelivery,
            null);

        result.ShouldBeFailure(OrderErrors.NegativeTotalAmount.Code);
    }
    
    [Test]
    public void Create_WithFinancialMismatch_ShouldFailWithFinancialMismatchError()
    {
        var subtotal = new Money(DefaultOrderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var correctTotalAmount = subtotal - DefaultDiscountAmount + DefaultDeliveryFee + DefaultTipAmount + DefaultTaxAmount;
        var incorrectTotalAmount = new Money(correctTotalAmount.Amount + 10m, Currencies.Default);

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
            incorrectTotalAmount,
            PaymentMethodType.CashOnDelivery,
            null);

        result.ShouldBeFailure(OrderErrors.FinancialMismatch.Code);
    }
}
