using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderPropertyImmutabilityTests : OrderTestHelpers
{
    [Test]
    public void OrderItems_ShouldBeReadOnly()
    {
        // Arrange
        var orderItem = OrderItem.Create(
            YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique(),
            YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;
        
        var orderItems = new List<OrderItem> { orderItem };
        
        var subtotal = new Money(orderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        var totalAmount = subtotal;

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            orderItems, 
            DefaultSpecialInstructions,
            subtotal,
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            Money.Zero(Currencies.Default),
            totalAmount,
            PaymentMethodType.CashOnDelivery,
            null);
            
        result.ShouldBeSuccessful();
        var order = result.Value;

        // Assert
        var property = typeof(Order).GetProperty(nameof(Order.OrderItems));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<OrderItem>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        Action mutate = () => ((ICollection<OrderItem>)order.OrderItems).Add(orderItem);
        mutate.Should().Throw<NotSupportedException>();

        orderItems.Add(CreateDefaultOrderItems().First());
        order.OrderItems.Should().HaveCount(1);
        
        order.OrderItems.First().Id.Should().Be(orderItem.Id);
    }

    [Test]
    public void PaymentTransactions_ShouldBeReadOnly()
    {
        // Arrange
        var order = CreateValidOrder();

        // Assert
        var property = typeof(Order).GetProperty(nameof(Order.PaymentTransactions));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<PaymentTransaction>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        Action mutate = () => ((ICollection<PaymentTransaction>)order.PaymentTransactions).Add(
            PaymentTransaction.Create(
                PaymentMethodType.CreditCard,
                PaymentTransactionType.Payment,
                new Money(10.00m, Currencies.Default),
                DateTime.UtcNow,
                "test_payment_id",
                "stripe").Value);
        mutate.Should().Throw<NotSupportedException>();
    }
}
