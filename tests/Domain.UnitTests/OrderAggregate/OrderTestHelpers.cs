using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

public static class OrderTestHelpers
{
    public static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    public static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    public static readonly DeliveryAddress DefaultDeliveryAddress = CreateDefaultDeliveryAddress();
    public static readonly List<OrderItem> DefaultOrderItems = CreateDefaultOrderItems();
    public const string DefaultSpecialInstructions = "No special instructions";
    public static readonly Money DefaultDiscountAmount = Money.Zero(Currencies.Default);
    public static readonly Money DefaultDeliveryFee = new Money(5.00m, Currencies.Default);
    public static readonly Money DefaultTipAmount = new Money(2.00m, Currencies.Default);
    public static readonly Money DefaultTaxAmount = new Money(1.50m, Currencies.Default);
    public const string DefaultPaymentGatewayReferenceId = "pi_test_123456789";

    /// <summary>
    /// Creates a valid order with default values and proper financial calculations.
    /// This helper now creates a Cash on Delivery order by default, which is immediately considered 'Placed'.
    /// </summary>
    public static Order CreateValidOrder()
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
            PaymentMethodType.CashOnDelivery, // Default to COD for a 'Placed' order
            null);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    /// <summary>
    /// Creates an order in the AwaitingPayment status for online payments.
    /// </summary>
    public static Order CreateAwaitingPaymentOrder(string? paymentGatewayReferenceId = null)
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
            PaymentMethodType.CreditCard, // Online payment method
            null,
            paymentGatewayReferenceId: paymentGatewayReferenceId ?? DefaultPaymentGatewayReferenceId);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    /// <summary>
    /// Creates an order in the Accepted status with a specific timestamp
    /// </summary>
    public static Order CreateAcceptedOrder(DateTime? timestamp = null)
    {
        var order = CreateValidOrder();
        var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);
        var result = order.Accept(estimatedDeliveryTime, timestamp ?? DateTime.UtcNow);
        result.ShouldBeSuccessful();
        return order;
    }

    /// <summary>
    /// Creates an order in the Preparing status with a specific timestamp
    /// </summary>
    public static Order CreatePreparingOrder(DateTime? timestamp = null)
    {
        var order = CreateAcceptedOrder(timestamp);
        var result = order.MarkAsPreparing(timestamp);
        result.ShouldBeSuccessful();
        return order;
    }

    /// <summary>
    /// Creates an order in the ReadyForDelivery status with a specific timestamp
    /// </summary>
    public static Order CreateReadyForDeliveryOrder(DateTime? timestamp = null)
    {
        var order = CreatePreparingOrder(timestamp);
        var result = order.MarkAsReadyForDelivery(timestamp);
        result.ShouldBeSuccessful();
        return order;
    }

    /// <summary>
    /// Creates a default delivery address for testing
    /// </summary>
    public static DeliveryAddress CreateDefaultDeliveryAddress()
    {
        return DeliveryAddress.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "USA").Value;
    }

    /// <summary>
    /// Creates default order items for testing
    /// </summary>
    public static List<OrderItem> CreateDefaultOrderItems()
    {
        var orderItem = OrderItem.Create(
            MenuCategoryId.CreateUnique(),
            MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;

        return new List<OrderItem> { orderItem };
    }
}
