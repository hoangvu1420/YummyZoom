using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class OrderFromTeamCartTests : OrderTestHelpers
{
    private TeamCartId _teamCartId = null!;
    private List<PaymentTransaction> _paymentTransactions = null!;
    private List<PaymentTransaction> _paymentTransactionsWithWrongTotal = null!;

    [SetUp]
    public void SetUp()
    {
        _teamCartId = TeamCartId.CreateUnique();
        _paymentTransactions = CreatePaymentTransactionsWithPaidByUserId();
        _paymentTransactionsWithWrongTotal = CreatePaymentTransactionsWithWrongTotal();
    }

    #region Enhanced Order.Create() Tests

    [Test]
    public void Create_WithTeamCartId_ShouldSetSourceTeamCartId()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            sourceTeamCartId: _teamCartId);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.SourceTeamCartId.Should().Be(_teamCartId);
    }

    [Test]
    public void Create_WithPaymentTransactions_ShouldAddTransactions()
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
            DefaultTaxAmount,
            null,
            null,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(_paymentTransactions.Count);
        order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
    }

    [Test]
    public void Create_WithTeamCartIdAndPaymentTransactions_ShouldSetBothCorrectly()
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
            DefaultTaxAmount,
            null,
            _teamCartId,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.SourceTeamCartId.Should().Be(_teamCartId);
        order.PaymentTransactions.Should().HaveCount(_paymentTransactions.Count);
        order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
    }

    [Test]
    public void Create_WithNullTeamCartId_ShouldLeaveSourceTeamCartIdNull()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            sourceTeamCartId: null);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.SourceTeamCartId.Should().BeNull();
    }

    [Test]
    public void Create_WithEmptyPaymentTransactions_ShouldNotAddTransactions()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            paymentTransactions: new List<PaymentTransaction>());

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.PaymentTransactions.Should().BeEmpty();
    }

    [Test]
    public void Create_WithNullPaymentTransactions_ShouldNotAddTransactions()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            paymentTransactions: null);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.PaymentTransactions.Should().BeEmpty();
    }

    #endregion

    #region Payment Transaction Validation Tests

    [Test]
    public void Create_WithMismatchedPaymentTotal_ShouldFailWithPaymentMismatchError()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            paymentTransactions: _paymentTransactionsWithWrongTotal);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.PaymentMismatch);
    }

    [Test]
    public void Create_WithCorrectPaymentTotal_ShouldSucceed()
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
            DefaultTaxAmount,
            null,
            null,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        var paymentTotal = order.PaymentTransactions.Sum(pt => pt.Amount.Amount);
        paymentTotal.Should().Be(order.TotalAmount.Amount);
    }

    [Test]
    public void Create_WithNoPaymentTransactions_ShouldSucceed()
    {
        // Arrange & Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            paymentTransactions: null);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.PaymentTransactions.Should().BeEmpty();
    }

    [Test]
    public void Create_WithPartialPaymentTransactions_ShouldFailWithPaymentMismatchError()
    {
        // Arrange
        var partialPayments = new List<PaymentTransaction>
        {
            _paymentTransactions.First() // Only one payment, not covering full total
        };

        // Act
        var result = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            paymentTransactions: partialPayments);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.PaymentMismatch);
    }

    #endregion

    #region Order Properties Tests

    [Test]
    public void Create_WithTeamCartData_ShouldMaintainAllExistingProperties()
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
            DefaultTaxAmount,
            null,
            _teamCartId,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.CustomerId.Should().Be(DefaultCustomerId);
        order.RestaurantId.Should().Be(DefaultRestaurantId);
        order.DeliveryAddress.Should().Be(DefaultDeliveryAddress);
        order.SpecialInstructions.Should().Be(DefaultSpecialInstructions);
        order.DiscountAmount.Should().Be(DefaultDiscountAmount);
        order.DeliveryFee.Should().Be(DefaultDeliveryFee);
        order.TipAmount.Should().Be(DefaultTipAmount);
        order.TaxAmount.Should().Be(DefaultTaxAmount);
        order.OrderItems.Should().BeEquivalentTo(DefaultOrderItems);
        order.SourceTeamCartId.Should().Be(_teamCartId);
        order.PaymentTransactions.Should().BeEquivalentTo(_paymentTransactions);
    }

    [Test]
    public void Create_WithTeamCartData_ShouldCalculateCorrectTotalAmount()
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
            DefaultTaxAmount,
            null,
            _teamCartId,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        var expectedTotal = DefaultOrderItems.Sum(item => item.LineItemTotal.Amount) 
                           + DefaultDeliveryFee.Amount 
                           + DefaultTipAmount.Amount 
                           + DefaultTaxAmount.Amount 
                           - DefaultDiscountAmount.Amount;
        
        order.TotalAmount.Amount.Should().Be(expectedTotal);
    }

    [Test]
    public void Create_WithTeamCartData_ShouldSetCorrectOrderStatus()
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
            DefaultTaxAmount,
            null,
            _teamCartId,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        order.Status.Should().Be(OrderStatus.Placed);
    }

    #endregion

    #region PaymentTransaction Properties Tests

    [Test]
    public void Create_WithPaymentTransactions_ShouldPreservePaidByUserId()
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
            DefaultTaxAmount,
            null,
            null,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        var firstTransaction = order.PaymentTransactions.First();
        firstTransaction.PaidByUserId.Should().NotBeNull();
        firstTransaction.PaidByUserId.Should().Be(_paymentTransactions.First().PaidByUserId);
    }

    [Test]
    public void Create_WithPaymentTransactions_ShouldPreserveAllTransactionProperties()
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
            DefaultTaxAmount,
            null,
            null,
            _paymentTransactions);

        // Assert
        result.ShouldBeSuccessful();
        var order = result.Value;
        
        for (int i = 0; i < _paymentTransactions.Count; i++)
        {
            var expected = _paymentTransactions[i];
            var actual = order.PaymentTransactions.ElementAt(i);
            
            actual.PaymentMethodType.Should().Be(expected.PaymentMethodType);
            actual.Amount.Should().Be(expected.Amount);
            actual.Type.Should().Be(expected.Type);
            actual.PaidByUserId.Should().Be(expected.PaidByUserId);
            actual.PaymentMethodDisplay.Should().Be(expected.PaymentMethodDisplay);
            actual.PaymentGatewayReferenceId.Should().Be(expected.PaymentGatewayReferenceId);
        }
    }

    #endregion

    #region Helper Methods

    private List<PaymentTransaction> CreatePaymentTransactionsWithPaidByUserId()
    {
        var transactions = new List<PaymentTransaction>();
        
        // Calculate what the total order amount should be
        var expectedTotal = DefaultOrderItems.Sum(item => item.LineItemTotal.Amount) 
                           + DefaultDeliveryFee.Amount 
                           + DefaultTipAmount.Amount 
                           + DefaultTaxAmount.Amount 
                           - DefaultDiscountAmount.Amount;
        
        // Create transactions that match the total - round to avoid floating point precision issues
        var firstAmount = Math.Round(expectedTotal * 0.6m, 2); // 60% of total
        var secondAmount = Math.Round(expectedTotal - firstAmount, 2); // Remainder to ensure exact match
        
        var firstTransaction = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(firstAmount, Currencies.Default),
            DateTime.UtcNow,
            "Online Payment",
            "txn_123",
            UserId.CreateUnique()).Value;
        
        var secondTransaction = PaymentTransaction.Create(
            PaymentMethodType.CashOnDelivery,
            PaymentTransactionType.Payment,
            new Money(secondAmount, Currencies.Default),
            DateTime.UtcNow,
            "Cash on Delivery",
            null,
            UserId.CreateUnique()).Value;
        
        transactions.Add(firstTransaction);
        transactions.Add(secondTransaction);
        
        return transactions;
    }

    private List<PaymentTransaction> CreatePaymentTransactionsWithWrongTotal()
    {
        var transactions = new List<PaymentTransaction>();
        
        // Create transactions that don't match the order total
        var transaction = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(10.00m, Currencies.Default), // Much less than order total
            DateTime.UtcNow,
            "Online Payment",
            "txn_123",
            UserId.CreateUnique()).Value;
        
        transactions.Add(transaction);
        
        return transactions;
    }

    #endregion
}
