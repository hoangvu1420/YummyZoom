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
        // Create a single order item directly
        var orderItem = OrderItem.Create(
            YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique(),
            YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;
        
        // Create a new list with a single item
        var orderItems = new List<OrderItem> { orderItem };
        
        // Calculate subtotal from order items
        var subtotal = new Money(orderItems.Sum(item => item.LineItemTotal.Amount), Currencies.Default);
        
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
            orderItems, 
            DefaultSpecialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            totalAmount,
            paymentTransactions,
            null);
            
        // Ensure order creation was successful
        result.IsSuccess.Should().BeTrue();
        var order = result.Value;

        // Assert
        // Type check
        var property = typeof(Order).GetProperty(nameof(Order.OrderItems));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<OrderItem>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<OrderItem>)order.OrderItems).Add(orderItem);
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the order
        orderItems.Add(CreateDefaultOrderItems().First());
        order.OrderItems.Should().HaveCount(1);
        
        // Verify that the item in the order is the same as our original item
        order.OrderItems.First().Id.Should().Be(orderItem.Id);
    }

    [Test]
    public void PaymentTransactions_ShouldBeReadOnly()
    {
        // Arrange
        var order = CreateValidOrder();

        // Assert
        // Type check
        var property = typeof(Order).GetProperty(nameof(Order.PaymentTransactions));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<PaymentTransaction>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
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
