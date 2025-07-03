using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

[TestFixture]
public class PaymentTransactionTests
{
    private static readonly PaymentMethodType DefaultPaymentMethodType = PaymentMethodType.CreditCard;
    private static readonly PaymentTransactionType DefaultTransactionType = PaymentTransactionType.Payment;
    private static readonly Money DefaultAmount = new Money(25.99m, Currencies.Default);
    private static readonly DateTime DefaultTimestamp = DateTime.UtcNow;
    private const string DefaultPaymentMethodDisplay = "Visa **** 4242";
    private const string DefaultGatewayReferenceId = "stripe_pi_12345";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.Id.Value.Should().NotBe(Guid.Empty);
        payment.PaymentMethodType.Should().Be(DefaultPaymentMethodType);
        payment.Type.Should().Be(DefaultTransactionType);
        payment.Amount.Should().Be(DefaultAmount);
        payment.Timestamp.Should().Be(DefaultTimestamp);
        payment.Status.Should().Be(PaymentStatus.Pending); // Initial status should be Pending
        payment.PaymentMethodDisplay.Should().BeNull();
        payment.PaymentGatewayReferenceId.Should().BeNull();
    }

    [Test]
    public void Create_WithAllOptionalParameters_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp,
            DefaultPaymentMethodDisplay,
            DefaultGatewayReferenceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaymentMethodDisplay.Should().Be(DefaultPaymentMethodDisplay);
        payment.PaymentGatewayReferenceId.Should().Be(DefaultGatewayReferenceId);
    }

    [Test]
    public void Create_WithRefundType_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Refund,
            DefaultAmount,
            DefaultTimestamp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        payment.Type.Should().Be(PaymentTransactionType.Refund);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-25.99)]
    public void Create_WithInvalidAmount_ShouldFailWithInvalidAmountError(decimal invalidAmount)
    {
        // Arrange
        var invalidAmountMoney = new Money(invalidAmount, Currencies.Default);

        // Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            invalidAmountMoney,
            DefaultTimestamp);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.PaymentTransactionInvalidAmount);
    }

    #endregion

    #region Status Management Tests

    [Test]
    public void MarkAsSucceeded_ShouldUpdateStatusToSucceeded()
    {
        // Arrange
        var payment = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp).Value;

        // Act
        payment.MarkAsSucceeded();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Test]
    public void MarkAsFailed_ShouldUpdateStatusToFailed()
    {
        // Arrange
        var payment = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp).Value;

        // Act
        payment.MarkAsFailed();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Test]
    public void MarkAsSucceeded_AfterMarkingAsFailed_ShouldUpdateToSucceeded()
    {
        // Arrange
        var payment = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp).Value;
        
        payment.MarkAsFailed();

        // Act
        payment.MarkAsSucceeded();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Test]
    public void MarkAsFailed_AfterMarkingAsSucceeded_ShouldUpdateToFailed()
    {
        // Arrange
        var payment = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp).Value;
        
        payment.MarkAsSucceeded();

        // Act
        payment.MarkAsFailed();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    #endregion

    #region Payment Method Type Tests

    [TestCase(PaymentMethodType.CreditCard)]
    [TestCase(PaymentMethodType.PayPal)]
    [TestCase(PaymentMethodType.ApplePay)]
    [TestCase(PaymentMethodType.GooglePay)]
    [TestCase(PaymentMethodType.CashOnDelivery)]
    public void Create_WithDifferentPaymentMethodTypes_ShouldSucceed(PaymentMethodType paymentMethodType)
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            paymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethodType.Should().Be(paymentMethodType);
    }

    #endregion

    #region Transaction Type Tests

    [TestCase(PaymentTransactionType.Payment)]
    [TestCase(PaymentTransactionType.Refund)]
    public void Create_WithDifferentTransactionTypes_ShouldSucceed(PaymentTransactionType transactionType)
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            transactionType,
            DefaultAmount,
            DefaultTimestamp);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(transactionType);
    }

    #endregion
}
