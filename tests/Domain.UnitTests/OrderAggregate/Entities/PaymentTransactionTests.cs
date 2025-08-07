using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate.Entities;

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
        result.ShouldBeFailure();
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

    #region PaidByUserId Property Tests

    [Test]
    public void Create_WithPaidByUserId_ShouldSetPaidByUserIdCorrectly()
    {
        // Arrange
        var paidByUserId = UserId.CreateUnique();

        // Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp,
            paidByUserId: paidByUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaidByUserId.Should().Be(paidByUserId);
    }

    [Test]
    public void Create_WithNullPaidByUserId_ShouldAllowNullPaidByUserId()
    {
        // Arrange & Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp,
            paidByUserId: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaidByUserId.Should().BeNull();
    }

    [Test]
    public void Create_WithAllParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var paidByUserId = UserId.CreateUnique();

        // Act
        var result = PaymentTransaction.Create(
            DefaultPaymentMethodType,
            DefaultTransactionType,
            DefaultAmount,
            DefaultTimestamp,
            DefaultPaymentMethodDisplay,
            DefaultGatewayReferenceId,
            paidByUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaymentMethodType.Should().Be(DefaultPaymentMethodType);
        payment.Type.Should().Be(DefaultTransactionType);
        payment.Amount.Should().Be(DefaultAmount);
        payment.Timestamp.Should().Be(DefaultTimestamp);
        payment.PaymentMethodDisplay.Should().Be(DefaultPaymentMethodDisplay);
        payment.PaymentGatewayReferenceId.Should().Be(DefaultGatewayReferenceId);
        payment.PaidByUserId.Should().Be(paidByUserId);
    }

    #endregion

    #region TeamCart Integration Tests

    [Test]
    public void Create_WithTeamCartMemberPayment_ShouldSetCorrectPaidByUserId()
    {
        // Arrange
        var memberId = UserId.CreateUnique();
        var memberPaymentAmount = new Money(25.00m, Currencies.Default);

        // Act
        var result = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            memberPaymentAmount,
            DateTime.UtcNow,
            "Online Payment",
            "stripe_pi_123",
            memberId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaidByUserId.Should().Be(memberId);
        payment.PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
        payment.Amount.Should().Be(memberPaymentAmount);
        payment.PaymentMethodDisplay.Should().Be("Online Payment");
        payment.PaymentGatewayReferenceId.Should().Be("stripe_pi_123");
    }

    [Test]
    public void Create_WithHostAsCODGuarantor_ShouldSetHostAsPayerId()
    {
        // Arrange
        var hostId = UserId.CreateUnique();
        var codAmount = new Money(50.00m, Currencies.Default);

        // Act
        var result = PaymentTransaction.Create(
            PaymentMethodType.CashOnDelivery,
            PaymentTransactionType.Payment,
            codAmount,
            DateTime.UtcNow,
            "Cash on Delivery",
            null,
            hostId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payment = result.Value;
        
        payment.PaidByUserId.Should().Be(hostId);
        payment.PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        payment.Amount.Should().Be(codAmount);
        payment.PaymentMethodDisplay.Should().Be("Cash on Delivery");
        payment.PaymentGatewayReferenceId.Should().BeNull();
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
