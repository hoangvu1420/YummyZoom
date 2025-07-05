using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
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
        var orderItems = new List<OrderItem> { CreateDefaultOrderItems().First() };

        // Act
        var order = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            orderItems,
            DefaultSpecialInstructions).Value;

        // Assert
        // Type check
        var property = typeof(Order).GetProperty(nameof(Order.OrderItems));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<OrderItem>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<OrderItem>)order.OrderItems).Add(CreateDefaultOrderItems().First());
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the order
        orderItems.Add(CreateDefaultOrderItems().First());
        order.OrderItems.Should().HaveCount(1);
    }

    [Test]
    public void AppliedCouponIds_ShouldBeReadOnly()
    {
        // Arrange
        var couponIds = new List<CouponId> { CouponId.CreateUnique() };

        // Act
        var order = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            appliedCouponIds: couponIds).Value;

        // Assert
        // Type check
        var property = typeof(Order).GetProperty(nameof(Order.AppliedCouponIds));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<CouponId>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<CouponId>)order.AppliedCouponIds).Add(CouponId.CreateUnique());
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the order
        couponIds.Add(CouponId.CreateUnique());
        order.AppliedCouponIds.Should().HaveCount(1);
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
