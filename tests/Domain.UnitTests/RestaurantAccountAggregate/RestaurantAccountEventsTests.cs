using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAccountAggregate;

[TestFixture]
public class RestaurantAccountEventsTests
{
    private static readonly RestaurantAccountId DefaultAccountId = RestaurantAccountId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly AccountTransactionId DefaultTransactionId = AccountTransactionId.CreateUnique();
    private static readonly Money DefaultAmount = new Money(10.00m, Currencies.Default);
    private static readonly OrderId DefaultOrderId = OrderId.Create(Guid.NewGuid());
    private static readonly PayoutMethodDetails DefaultPayoutMethod = PayoutMethodDetails.Create("Bank Account: ****1234").Value;

    #region RestaurantAccountCreated Event Tests

    [Test]
    public void RestaurantAccountCreated_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var accountCreatedEvent = new RestaurantAccountCreated(DefaultAccountId, DefaultRestaurantId);

        // Assert
        accountCreatedEvent.RestaurantAccountId.Should().Be(DefaultAccountId);
        accountCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
    }

    [Test]
    public void RestaurantAccountCreated_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var accountCreatedEvent = new RestaurantAccountCreated(DefaultAccountId, DefaultRestaurantId);

        // Assert
        accountCreatedEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    #endregion

    #region TransactionAdded Event Tests

    [Test]
    public void TransactionAdded_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var transactionAddedEvent = new TransactionAdded(
            DefaultAccountId,
            DefaultTransactionId,
            TransactionType.OrderRevenue,
            DefaultAmount,
            DefaultOrderId);

        // Assert
        transactionAddedEvent.RestaurantAccountId.Should().Be(DefaultAccountId);
        transactionAddedEvent.TransactionId.Should().Be(DefaultTransactionId);
        transactionAddedEvent.Type.Should().Be(TransactionType.OrderRevenue);
        transactionAddedEvent.Amount.Should().Be(DefaultAmount);
        transactionAddedEvent.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void TransactionAdded_WithNullOrderId_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var transactionAddedEvent = new TransactionAdded(
            DefaultAccountId,
            DefaultTransactionId,
            TransactionType.ManualAdjustment,
            DefaultAmount,
            null);

        // Assert
        transactionAddedEvent.RestaurantAccountId.Should().Be(DefaultAccountId);
        transactionAddedEvent.TransactionId.Should().Be(DefaultTransactionId);
        transactionAddedEvent.Type.Should().Be(TransactionType.ManualAdjustment);
        transactionAddedEvent.Amount.Should().Be(DefaultAmount);
        transactionAddedEvent.RelatedOrderId.Should().BeNull();
    }

    [Test]
    public void TransactionAdded_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var transactionAddedEvent = new TransactionAdded(
            DefaultAccountId,
            DefaultTransactionId,
            TransactionType.OrderRevenue,
            DefaultAmount,
            DefaultOrderId);

        // Assert
        transactionAddedEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [TestCase(TransactionType.OrderRevenue)]
    [TestCase(TransactionType.PlatformFee)]
    [TestCase(TransactionType.RefundDeduction)]
    [TestCase(TransactionType.PayoutSettlement)]
    [TestCase(TransactionType.ManualAdjustment)]
    public void TransactionAdded_WithDifferentTransactionTypes_ShouldInitializeCorrectly(TransactionType transactionType)
    {
        // Arrange & Act
        var transactionAddedEvent = new TransactionAdded(
            DefaultAccountId,
            DefaultTransactionId,
            transactionType,
            DefaultAmount,
            DefaultOrderId);

        // Assert
        transactionAddedEvent.Type.Should().Be(transactionType);
    }

    #endregion

    #region PayoutSettled Event Tests

    [Test]
    public void PayoutSettled_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var payoutAmount = new Money(50.00m, Currencies.Default);
        var newBalance = new Money(25.00m, Currencies.Default);

        // Act
        var payoutSettledEvent = new PayoutSettled(DefaultAccountId, payoutAmount, newBalance);

        // Assert
        payoutSettledEvent.RestaurantAccountId.Should().Be(DefaultAccountId);
        payoutSettledEvent.PayoutAmount.Should().Be(payoutAmount);
        payoutSettledEvent.NewBalance.Should().Be(newBalance);
    }

    [Test]
    public void PayoutSettled_ShouldImplementIDomainEvent()
    {
        // Arrange
        var payoutAmount = new Money(50.00m, Currencies.Default);
        var newBalance = new Money(25.00m, Currencies.Default);

        // Act
        var payoutSettledEvent = new PayoutSettled(DefaultAccountId, payoutAmount, newBalance);

        // Assert
        payoutSettledEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [Test]
    public void PayoutSettled_WithZeroNewBalance_ShouldInitializeCorrectly()
    {
        // Arrange
        var payoutAmount = new Money(100.00m, Currencies.Default);
        var zeroBalance = new Money(0.00m, Currencies.Default);

        // Act
        var payoutSettledEvent = new PayoutSettled(DefaultAccountId, payoutAmount, zeroBalance);

        // Assert
        payoutSettledEvent.PayoutAmount.Should().Be(payoutAmount);
        payoutSettledEvent.NewBalance.Should().Be(zeroBalance);
    }

    #endregion

    #region PayoutMethodUpdated Event Tests

    [Test]
    public void PayoutMethodUpdated_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var payoutMethodUpdatedEvent = new PayoutMethodUpdated(DefaultAccountId, DefaultPayoutMethod);

        // Assert
        payoutMethodUpdatedEvent.RestaurantAccountId.Should().Be(DefaultAccountId);
        payoutMethodUpdatedEvent.NewPayoutMethod.Should().Be(DefaultPayoutMethod);
    }

    [Test]
    public void PayoutMethodUpdated_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var payoutMethodUpdatedEvent = new PayoutMethodUpdated(DefaultAccountId, DefaultPayoutMethod);

        // Assert
        payoutMethodUpdatedEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [Test]
    public void PayoutMethodUpdated_WithDifferentPayoutMethods_ShouldInitializeCorrectly()
    {
        // Arrange
        var paypalMethod = PayoutMethodDetails.Create("PayPal: user@example.com").Value;
        var bankMethod = PayoutMethodDetails.Create("Bank Account: ****5678").Value;

        // Act
        var paypalEvent = new PayoutMethodUpdated(DefaultAccountId, paypalMethod);
        var bankEvent = new PayoutMethodUpdated(DefaultAccountId, bankMethod);

        // Assert
        paypalEvent.NewPayoutMethod.Should().Be(paypalMethod);
        bankEvent.NewPayoutMethod.Should().Be(bankMethod);
        paypalEvent.NewPayoutMethod.Should().NotBe(bankEvent.NewPayoutMethod);
    }

    #endregion

    #region Event Equality Tests (Records)

    [Test]
    public void RestaurantAccountCreated_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var event1 = new RestaurantAccountCreated(DefaultAccountId, DefaultRestaurantId);
        var event2 = new RestaurantAccountCreated(DefaultAccountId, DefaultRestaurantId);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
        (event1 == event2).Should().BeTrue();
    }

    [Test]
    public void TransactionAdded_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var event1 = new TransactionAdded(DefaultAccountId, DefaultTransactionId, TransactionType.OrderRevenue, DefaultAmount, DefaultOrderId);
        var event2 = new TransactionAdded(DefaultAccountId, DefaultTransactionId, TransactionType.OrderRevenue, DefaultAmount, DefaultOrderId);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
    }

    [Test]
    public void PayoutSettled_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var payoutAmount = new Money(50.00m, Currencies.Default);
        var newBalance = new Money(25.00m, Currencies.Default);
        var event1 = new PayoutSettled(DefaultAccountId, payoutAmount, newBalance);
        var event2 = new PayoutSettled(DefaultAccountId, payoutAmount, newBalance);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
    }

    [Test]
    public void PayoutMethodUpdated_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var event1 = new PayoutMethodUpdated(DefaultAccountId, DefaultPayoutMethod);
        var event2 = new PayoutMethodUpdated(DefaultAccountId, DefaultPayoutMethod);

        // Act & Assert
        event1.Should().Be(event2);
        event1.Equals(event2).Should().BeTrue();
    }

    #endregion
}
