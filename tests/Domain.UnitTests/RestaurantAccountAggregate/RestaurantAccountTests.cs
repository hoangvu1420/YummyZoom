using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAccountAggregate;

[TestFixture]
public class RestaurantAccountTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly Money TenDollars = new Money(10.00m, Currencies.Default);
    private static readonly Money FiveDollars = new Money(5.00m, Currencies.Default);
    private static readonly Money NegativeFiveDollars = new Money(-5.00m, Currencies.Default);
    private static readonly Money ZeroDollars = new Money(0.00m, Currencies.Default);
    private static readonly OrderId DefaultOrderId = OrderId.Create(Guid.NewGuid());
    private static readonly Guid DefaultAdminId = Guid.NewGuid();
    private static readonly string DefaultReason = "Test adjustment";

    private static PayoutMethodDetails CreateTestPayoutMethod()
    {
        return PayoutMethodDetails.Create("Bank Account: ****1234").Value;
    }

    #region Create() Method Tests

    [Test]
    public void Create_WithValidRestaurantId_ShouldReturnSuccessfulResult()
    {
        // Arrange & Act
        var result = RestaurantAccount.Create(DefaultRestaurantId);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().NotBeNull();
        result.Value.RestaurantId.Should().Be(DefaultRestaurantId);
    }

    [Test]
    public void Create_WithValidRestaurantId_ShouldSucceedAndInitializeAccountCorrectly()
    {
        // Arrange & Act
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();

        // Assert
        account.Should().NotBeNull();
        account.Id.Value.Should().NotBe(Guid.Empty);
        account.RestaurantId.Should().Be(DefaultRestaurantId);
        account.CurrentBalance.Should().Be(ZeroDollars);
        account.PayoutMethodDetails.Should().BeNull();
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantAccountCreated));

        var accountCreatedEvent = account.DomainEvents.OfType<RestaurantAccountCreated>().Single();
        accountCreatedEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        accountCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
    }

    #endregion

    #region RecordRevenue() Method Tests

    [Test]
    public void RecordRevenue_WithValidPositiveAmount_ShouldSucceedAndUpdateBalanceAndRaiseEvent()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.RecordRevenue(TenDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(TenDollars);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RevenueRecorded));
        
        var revenueRecordedEvent = account.DomainEvents.OfType<RevenueRecorded>().Single();
        revenueRecordedEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        revenueRecordedEvent.Amount.Should().Be(TenDollars);
        revenueRecordedEvent.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void RecordRevenue_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var initialBalance = account.CurrentBalance;
        account.ClearDomainEvents();

        // Act
        var result = account.RecordRevenue(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive(NegativeFiveDollars));
        account.CurrentBalance.Should().Be(initialBalance);
        account.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void RecordRevenue_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.RecordRevenue(ZeroDollars, DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive(ZeroDollars));
    }

    #endregion

    #region RecordPlatformFee() Method Tests

    [Test]
    public void RecordPlatformFee_WithValidNegativeAmount_ShouldSucceedAndUpdateBalance()
    {
        // Arrange
        var account = CreateAccountWithBalance(10.00m);

        // Act
        var result = account.RecordPlatformFee(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(PlatformFeeRecorded));
    }

    [Test]
    public void RecordPlatformFee_WithPositiveAmount_ShouldFail()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.RecordPlatformFee(FiveDollars, DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.PlatformFeeMustBeNegative(FiveDollars));
        account.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region RecordRefundDeduction() Method Tests

    [Test]
    public void RecordRefundDeduction_WithValidNegativeAmount_ShouldSucceedAndUpdateBalance()
    {
        // Arrange
        var account = CreateAccountWithBalance(10.00m);

        // Act
        var result = account.RecordRefundDeduction(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RefundDeducted));
    }

    [Test]
    public void RecordRefundDeduction_WithPositiveAmount_ShouldFail()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.RecordRefundDeduction(FiveDollars, DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.RefundDeductionMustBeNegative(FiveDollars));
    }

    #endregion

    #region SettlePayout() Method Tests

    [Test]
    public void SettlePayout_WithValidAmountNotExceedingBalance_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var account = CreateAccountWithBalance(10.00m);

        // Act
        var result = account.SettlePayout(FiveDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(PayoutSettled));

        var payoutSettledEvent = account.DomainEvents.OfType<PayoutSettled>().Single();
        payoutSettledEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        payoutSettledEvent.PayoutAmount.Should().Be(FiveDollars);
        payoutSettledEvent.NewBalance.Amount.Should().Be(5.00m);
    }

    [Test]
    public void SettlePayout_WithAmountExceedingBalance_ShouldFail()
    {
        // Arrange
        var account = CreateAccountWithBalance(5.00m);
        var initialBalance = account.CurrentBalance;

        // Act
        var result = account.SettlePayout(TenDollars);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Code.Should().Be(RestaurantAccountErrors.InsufficientBalance(initialBalance, TenDollars).Code);
        account.CurrentBalance.Should().Be(initialBalance);
        account.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void SettlePayout_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var account = CreateAccountWithBalance(10.00m);

        // Act
        var result = account.SettlePayout(ZeroDollars);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.PayoutAmountMustBePositive(ZeroDollars));
    }

    #endregion

    #region MakeManualAdjustment() Method Tests

    [Test]
    public void MakeManualAdjustment_WithPositiveAmount_ShouldSucceedAndIncreaseBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.MakeManualAdjustment(TenDollars, DefaultReason, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(TenDollars);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(ManualAdjustmentMade));
    }

    [Test]
    public void MakeManualAdjustment_WithNegativeAmount_ShouldSucceedAndDecreaseBalance()
    {
        // Arrange
        var account = CreateAccountWithBalance(10.00m);

        // Act
        var result = account.MakeManualAdjustment(NegativeFiveDollars, DefaultReason, DefaultAdminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
    }

    [Test]
    public void MakeManualAdjustment_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();

        // Act
        var result = account.MakeManualAdjustment(TenDollars, string.Empty, DefaultAdminId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.ManualAdjustmentReasonRequired);
    }

    #endregion

    #region UpdatePayoutMethod() Method Tests

    [Test]
    public void UpdatePayoutMethod_WithValidDetails_ShouldSucceedAndUpdateValue()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var payoutMethod = CreateTestPayoutMethod();
        var result = account.UpdatePayoutMethod(payoutMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.PayoutMethodDetails.Should().Be(payoutMethod);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(PayoutMethodUpdated));
    }

    #endregion

    #region Helper Methods

    private static RestaurantAccount CreateAccountWithBalance(decimal balance)
    {
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        if (balance != 0)
        {
            var amount = new Money(balance, Currencies.Default);
            // Use manual adjustment for test setup to avoid unrelated events
            account.MakeManualAdjustment(amount, "Initial Balance", DefaultAdminId);
        }
        account.ClearDomainEvents();
        return account;
    }

    #endregion
}
