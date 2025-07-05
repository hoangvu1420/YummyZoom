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

public abstract class OrderTestHelpers
{
    protected static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    protected static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    protected static readonly DeliveryAddress DefaultDeliveryAddress = CreateDefaultDeliveryAddress();
    protected static readonly List<OrderItem> DefaultOrderItems = CreateDefaultOrderItems();
    protected const string DefaultSpecialInstructions = "No special instructions";
    protected static readonly Money DefaultDiscountAmount = Money.Zero(Currencies.Default);
    protected static readonly Money DefaultDeliveryFee = new Money(5.00m, Currencies.Default);
    protected static readonly Money DefaultTipAmount = new Money(2.00m, Currencies.Default);
    protected static readonly Money DefaultTaxAmount = new Money(1.50m, Currencies.Default);

    protected static Order CreateValidOrder()
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

    protected static Order CreateAcceptedOrder()
    {
        var order = CreateValidOrder();
        var result = order.Accept(DateTime.UtcNow.AddHours(1));
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static Order CreatePreparingOrder()
    {
        var order = CreateAcceptedOrder();
        var result = order.MarkAsPreparing();
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static Order CreateReadyForDeliveryOrder()
    {
        var order = CreatePreparingOrder();
        var result = order.MarkAsReadyForDelivery();
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static DeliveryAddress CreateDefaultDeliveryAddress()
    {
        return DeliveryAddress.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "USA").Value;
    }

    protected static List<OrderItem> CreateDefaultOrderItems()
    {
        var orderItem = OrderItem.Create(
            MenuCategoryId.CreateUnique(),
            MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;

        return new List<OrderItem> { orderItem };
    }

    protected static PaymentTransaction CreateValidPaymentTransaction()
    {
        return PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(25.50m, Currencies.Default),
            DateTime.UtcNow).Value;
    }
}
