using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate.Entities;

[TestFixture]
public class MemberPaymentTests
{
    private static readonly UserId DefaultUserId = UserId.CreateUnique();
    private static readonly Money DefaultAmount = new Money(25.50m, Currencies.Default);
    private const PaymentMethod DefaultOnlineMethod = PaymentMethod.Online;
    private const PaymentMethod DefaultCODMethod = PaymentMethod.CashOnDelivery;
    private const string DefaultTransactionId = "txn_123456789";

    [Test]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var userId = DefaultUserId;
        var amount = DefaultAmount;
        var method = DefaultOnlineMethod;

        // Act
        var result = MemberPayment.Create(userId, amount, method);

        // Assert
        result.ShouldBeSuccessful();
        var payment = result.Value;
        payment.UserId.Should().Be(userId);
        payment.Amount.Should().Be(amount);
        payment.Method.Should().Be(method);
        payment.Status.Should().Be(PaymentStatus.Pending); // Online starts as Pending
        payment.OnlineTransactionId.Should().BeNull();
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_WithCashOnDeliveryMethod_ShouldSetStatusToCommittedToCOD()
    {
        // Arrange
        var userId = DefaultUserId;
        var amount = new Money(30.00m, Currencies.Default);
        var method = DefaultCODMethod;

        // Act
        var result = MemberPayment.Create(userId, amount, method);

        // Assert
        result.ShouldBeSuccessful();
        var payment = result.Value;
        payment.Status.Should().Be(PaymentStatus.CommittedToCOD);
        payment.IsComplete().Should().BeTrue();
    }

    [Test]
    public void Create_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var userId = DefaultUserId;
        var amount = new Money(0, Currencies.Default);
        var method = DefaultOnlineMethod;

        // Act
        var result = MemberPayment.Create(userId, amount, method);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
    }

    [Test]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var userId = DefaultUserId;
        var amount = new Money(-10.00m, Currencies.Default);
        var method = DefaultOnlineMethod;

        // Act
        var result = MemberPayment.Create(userId, amount, method);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
    }

    [Test]
    public void MarkAsPaidOnline_WithValidTransactionId_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidOnlinePayment();
        var transactionId = DefaultTransactionId;

        // Act
        var result = payment.MarkAsPaidOnline(transactionId);

        // Assert
        result.ShouldBeSuccessful();
        payment.Status.Should().Be(PaymentStatus.PaidOnline);
        payment.OnlineTransactionId.Should().Be(transactionId);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.IsComplete().Should().BeTrue();
    }

    [Test]
    public void MarkAsPaidOnline_WithEmptyTransactionId_ShouldFail()
    {
        // Arrange
        var payment = CreateValidOnlinePayment();

        // Act
        var result = payment.MarkAsPaidOnline("");

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MemberPayment.InvalidTransactionId");
    }

    [Test]
    public void MarkAsPaidOnline_ForCODPayment_ShouldFail()
    {
        // Arrange
        var payment = CreateValidCODPayment();

        // Act
        var result = payment.MarkAsPaidOnline("txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MemberPayment.NotOnlinePayment");
    }

    [Test]
    public void MarkAsPaidOnline_WhenAlreadyPaid_ShouldBeIdempotent()
    {
        // Arrange
        var payment = CreateValidOnlinePayment();
        var transactionId = "txn_123456789";
        payment.MarkAsPaidOnline(transactionId);

        // Act
        var result = payment.MarkAsPaidOnline(transactionId);

        // Assert
        result.ShouldBeSuccessful();
        payment.Status.Should().Be(PaymentStatus.PaidOnline);
        payment.OnlineTransactionId.Should().Be(transactionId);
    }

    [Test]
    public void MarkAsFailed_ForOnlinePayment_ShouldSucceed()
    {
        // Arrange
        var payment = CreateValidOnlinePayment();

        // Act
        var result = payment.MarkAsFailed();

        // Assert
        result.ShouldBeSuccessful();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.HasFailed().Should().BeTrue();
        payment.IsComplete().Should().BeFalse();
    }

    [Test]
    public void MarkAsFailed_ForCODPayment_ShouldFail()
    {
        // Arrange
        var payment = CreateValidCODPayment();

        // Act
        var result = payment.MarkAsFailed();

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MemberPayment.NotOnlinePayment");
    }

    [Test]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldBeIdempotent()
    {
        // Arrange
        var payment = CreateValidOnlinePayment();
        payment.MarkAsFailed();

        // Act
        var result = payment.MarkAsFailed();

        // Assert
        result.ShouldBeSuccessful();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Test]
    public void GetStatusDisplayName_ShouldReturnCorrectDisplayNames()
    {
        // Arrange & Act & Assert
        var pendingPayment = CreateValidOnlinePayment();
        pendingPayment.GetStatusDisplayName().Should().Be("Pending Payment");

        var codPayment = CreateValidCODPayment();
        codPayment.GetStatusDisplayName().Should().Be("Cash on Delivery");

        var paidPayment = CreateValidOnlinePayment();
        paidPayment.MarkAsPaidOnline("txn_123");
        paidPayment.GetStatusDisplayName().Should().Be("Paid Online");

        var failedPayment = CreateValidOnlinePayment();
        failedPayment.MarkAsFailed();
        failedPayment.GetStatusDisplayName().Should().Be("Payment Failed");
    }

    [Test]
    public void IsComplete_ShouldReturnCorrectValues()
    {
        // Arrange & Act & Assert
        var pendingPayment = CreateValidOnlinePayment();
        pendingPayment.IsComplete().Should().BeFalse();

        var codPayment = CreateValidCODPayment();
        codPayment.IsComplete().Should().BeTrue();

        var paidPayment = CreateValidOnlinePayment();
        paidPayment.MarkAsPaidOnline("txn_123");
        paidPayment.IsComplete().Should().BeTrue();

        var failedPayment = CreateValidOnlinePayment();
        failedPayment.MarkAsFailed();
        failedPayment.IsComplete().Should().BeFalse();
    }

    [Test]
    public void HasFailed_ShouldReturnCorrectValues()
    {
        // Arrange & Act & Assert
        var pendingPayment = CreateValidOnlinePayment();
        pendingPayment.HasFailed().Should().BeFalse();

        var codPayment = CreateValidCODPayment();
        codPayment.HasFailed().Should().BeFalse();

        var paidPayment = CreateValidOnlinePayment();
        paidPayment.MarkAsPaidOnline("txn_123");
        paidPayment.HasFailed().Should().BeFalse();

        var failedPayment = CreateValidOnlinePayment();
        failedPayment.MarkAsFailed();
        failedPayment.HasFailed().Should().BeTrue();
    }

    #region Helper Methods

    private static MemberPayment CreateValidOnlinePayment()
    {
        var userId = DefaultUserId;
        var amount = DefaultAmount;
        return MemberPayment.Create(userId, amount, DefaultOnlineMethod).Value;
    }

    private static MemberPayment CreateValidCODPayment()
    {
        var userId = DefaultUserId;
        var amount = new Money(30.00m, Currencies.Default);
        return MemberPayment.Create(userId, amount, DefaultCODMethod).Value;
    }

    #endregion
}
