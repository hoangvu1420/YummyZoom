using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UnitTests;

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
        account.Transactions.Should().BeEmpty();
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantAccountCreated));

        var accountCreatedEvent = account.DomainEvents.OfType<RestaurantAccountCreated>().Single();
        accountCreatedEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        accountCreatedEvent.RestaurantId.Should().Be(DefaultRestaurantId);
    }

    #endregion

    #region AddOrderRevenue() Method Tests

    [Test]
    public void AddOrderRevenue_WithValidPositiveAmount_ShouldSucceedAndUpdateBalanceAndAddTransaction()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents(); // Clear creation event

        // Act
        var result = account.AddOrderRevenue(TenDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(TenDollars);
        account.Transactions.Should().HaveCount(1);
        
        var transaction = account.Transactions.First();
        transaction.Type.Should().Be(TransactionType.OrderRevenue);
        transaction.Amount.Should().Be(TenDollars);
        transaction.RelatedOrderId.Should().Be(DefaultOrderId);
        transaction.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TransactionAdded));
        var transactionAddedEvent = account.DomainEvents.OfType<TransactionAdded>().Single();
        transactionAddedEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        transactionAddedEvent.Type.Should().Be(TransactionType.OrderRevenue);
        transactionAddedEvent.Amount.Should().Be(TenDollars);
        transactionAddedEvent.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void AddOrderRevenue_WithNegativeAmount_ShouldFailWithOrderRevenueMustBePositiveError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var initialBalance = account.CurrentBalance;
        var initialTransactionCount = account.Transactions.Count;
        account.ClearDomainEvents();

        // Act
        var result = account.AddOrderRevenue(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive);
        account.CurrentBalance.Should().Be(initialBalance);
        account.Transactions.Should().HaveCount(initialTransactionCount);
        account.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AddOrderRevenue_WithZeroAmount_ShouldFailWithOrderRevenueMustBePositiveError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.AddOrderRevenue(ZeroDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive);
    }

    [Test]
    public void AddOrderRevenue_MultipleTransactions_ShouldAccumulateBalanceCorrectly()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result1 = account.AddOrderRevenue(TenDollars, DefaultOrderId);
        var result2 = account.AddOrderRevenue(FiveDollars, DefaultOrderId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(15.00m);
        account.Transactions.Should().HaveCount(2);
        account.DomainEvents.Should().HaveCount(2);
    }

    #endregion

    #region AddPlatformFee() Method Tests

    [Test]
    public void AddPlatformFee_WithValidNegativeAmount_ShouldSucceedAndUpdateBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // Add some revenue first
        account.ClearDomainEvents();

        // Act
        var result = account.AddPlatformFee(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        account.Transactions.Should().HaveCount(2);
        
        var platformFeeTransaction = account.Transactions.Last();
        platformFeeTransaction.Type.Should().Be(TransactionType.PlatformFee);
        platformFeeTransaction.Amount.Should().Be(NegativeFiveDollars);
        platformFeeTransaction.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void AddPlatformFee_WithPositiveAmount_ShouldFailWithPlatformFeeMustBeNegativeError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.AddPlatformFee(FiveDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.PlatformFeeMustBeNegative);
        account.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region AddRefundDeduction() Method Tests

    [Test]
    public void AddRefundDeduction_WithValidNegativeAmount_ShouldSucceedAndUpdateBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // Add some revenue first
        account.ClearDomainEvents();

        // Act
        var result = account.AddRefundDeduction(NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        
        var refundTransaction = account.Transactions.Last();
        refundTransaction.Type.Should().Be(TransactionType.RefundDeduction);
        refundTransaction.Amount.Should().Be(NegativeFiveDollars);
        refundTransaction.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void AddRefundDeduction_WithPositiveAmount_ShouldFailWithRefundDeductionMustBeNegativeError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.AddRefundDeduction(FiveDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.RefundDeductionMustBeNegative);
    }

    #endregion

    #region ProcessPayout() Method Tests

    [Test]
    public void ProcessPayout_WithValidAmountNotExceedingBalance_ShouldSucceedAndCreatePayoutTransaction()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // Balance = $10
        account.ClearDomainEvents();

        // Act
        var result = account.ProcessPayout(FiveDollars); // Payout $5

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m); // Remaining balance = $5
        
        var payoutTransaction = account.Transactions.Last();
        payoutTransaction.Type.Should().Be(TransactionType.PayoutSettlement);
        payoutTransaction.Amount.Should().Be(NegativeFiveDollars); // Stored as negative
        payoutTransaction.RelatedOrderId.Should().BeNull(); // Payouts not linked to specific orders

        // Should have both TransactionAdded and PayoutSettled events
        account.DomainEvents.Should().HaveCount(2);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TransactionAdded));
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(PayoutSettled));

        var payoutSettledEvent = account.DomainEvents.OfType<PayoutSettled>().Single();
        payoutSettledEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        payoutSettledEvent.PayoutAmount.Should().Be(FiveDollars); // Event stores original positive amount
        payoutSettledEvent.NewBalance.Amount.Should().Be(5.00m);
    }

    [Test]
    public void ProcessPayout_WithAmountExceedingBalance_ShouldFailWithInsufficientBalanceError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(FiveDollars, DefaultOrderId); // Balance = $5
        var initialBalance = account.CurrentBalance;
        var initialTransactionCount = account.Transactions.Count;
        account.ClearDomainEvents();

        // Act
        var result = account.ProcessPayout(TenDollars); // Try to payout $10 (more than $5 balance)

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.InsufficientBalance);
        account.CurrentBalance.Should().Be(initialBalance);
        account.Transactions.Should().HaveCount(initialTransactionCount);
        account.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void ProcessPayout_WithExactBalanceAmount_ShouldSucceedAndLeaveZeroBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // Balance = $10
        account.ClearDomainEvents();

        // Act
        var result = account.ProcessPayout(TenDollars); // Payout exactly $10

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(ZeroDollars);
    }

    [Test]
    public void ProcessPayout_WithZeroBalance_ShouldFailWithInsufficientBalanceError()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail(); // Balance = $0
        account.ClearDomainEvents();

        // Act
        var result = account.ProcessPayout(FiveDollars);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.InsufficientBalance);
    }

    #endregion

    #region AddManualAdjustment() Method Tests

    [Test]
    public void AddManualAdjustment_WithPositiveAmount_ShouldSucceedAndIncreaseBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.ClearDomainEvents();

        // Act
        var result = account.AddManualAdjustment(TenDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(TenDollars);
        
        var adjustmentTransaction = account.Transactions.Last();
        adjustmentTransaction.Type.Should().Be(TransactionType.ManualAdjustment);
        adjustmentTransaction.Amount.Should().Be(TenDollars);
        adjustmentTransaction.RelatedOrderId.Should().BeNull();
    }

    [Test]
    public void AddManualAdjustment_WithNegativeAmount_ShouldSucceedAndDecreaseBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // Start with $10
        account.ClearDomainEvents();

        // Act
        var result = account.AddManualAdjustment(NegativeFiveDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(5.00m);
        
        var adjustmentTransaction = account.Transactions.Last();
        adjustmentTransaction.Type.Should().Be(TransactionType.ManualAdjustment);
        adjustmentTransaction.Amount.Should().Be(NegativeFiveDollars);
    }

    [Test]
    public void AddManualAdjustment_WithZeroAmount_ShouldSucceedAndNotChangeBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var initialBalance = account.CurrentBalance;
        account.ClearDomainEvents();

        // Act
        var result = account.AddManualAdjustment(ZeroDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(initialBalance);
        account.Transactions.Should().HaveCount(1); // Transaction is still recorded
    }

    #endregion

    #region UpdatePayoutMethod() Method Tests

    [Test]
    public void UpdatePayoutMethod_WithValidPayoutMethodDetails_ShouldSucceedAndUpdatePayoutMethod()
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
        var payoutMethodUpdatedEvent = account.DomainEvents.OfType<PayoutMethodUpdated>().Single();
        payoutMethodUpdatedEvent.RestaurantAccountId.Should().Be((RestaurantAccountId)account.Id);
        payoutMethodUpdatedEvent.NewPayoutMethod.Should().Be(payoutMethod);
    }

    [Test]
    public void UpdatePayoutMethod_MultipleUpdates_ShouldOverwritePreviousPayoutMethod()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var firstPayoutMethod = PayoutMethodDetails.Create("First Method").Value;
        var secondPayoutMethod = PayoutMethodDetails.Create("Second Method").Value;
        account.UpdatePayoutMethod(firstPayoutMethod);
        account.ClearDomainEvents();

        // Act
        var result = account.UpdatePayoutMethod(secondPayoutMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.PayoutMethodDetails.Should().Be(secondPayoutMethod);
        account.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(PayoutMethodUpdated));
    }

    #endregion

    #region Balance Consistency Invariant Tests

    [Test]
    public void BalanceConsistency_AfterMultipleTransactionTypes_ShouldMaintainCorrectBalance()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var twentyDollars = new Money(20.00m, Currencies.Default);
        var threeDollars = new Money(3.00m, Currencies.Default);
        var negativeThreeDollars = new Money(-3.00m, Currencies.Default);
        var negativeTwoDollars = new Money(-2.00m, Currencies.Default);

        // Act - Simulate a complete order lifecycle
        account.AddOrderRevenue(twentyDollars, DefaultOrderId); // +$20, Balance = $20
        account.AddPlatformFee(negativeThreeDollars, DefaultOrderId); // -$3, Balance = $17
        account.AddOrderRevenue(TenDollars, DefaultOrderId); // +$10, Balance = $27
        account.AddRefundDeduction(negativeTwoDollars, DefaultOrderId); // -$2, Balance = $25
        account.AddManualAdjustment(FiveDollars); // +$5, Balance = $30
        account.ProcessPayout(TenDollars); // -$10, Balance = $20

        // Assert
        account.CurrentBalance.Amount.Should().Be(20.00m);
        account.Transactions.Should().HaveCount(6);
        
        // Verify the calculation: 20 - 3 + 10 - 2 + 5 - 10 = 20
        var totalFromTransactions = account.Transactions.Sum(t => t.Amount.Amount);
        totalFromTransactions.Should().Be(20.00m);
    }

    [Test]
    public void BalanceConsistency_CurrentBalanceShouldAlwaysEqualSumOfTransactions()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var randomAmounts = new[]
        {
            new Money(15.75m, Currencies.Default),
            new Money(-2.25m, Currencies.Default),
            new Money(8.50m, Currencies.Default),
            new Money(-1.00m, Currencies.Default)
        };

        // Act & Assert - Check consistency after each transaction
        foreach (var amount in randomAmounts)
        {
            if (amount.Amount > 0)
            {
                account.AddOrderRevenue(amount, DefaultOrderId);
            }
            else
            {
                account.AddManualAdjustment(amount);
            }

            // Verify invariant after each transaction
            var calculatedBalance = account.Transactions.Sum(t => t.Amount.Amount);
            account.CurrentBalance.Amount.Should().Be(calculatedBalance);
        }
    }

    #endregion

    #region Integration Tests - Real-world Scenarios

    [Test]
    public void RestaurantAccount_CompleteLifecycle_ShouldHandleAllOperationsCorrectly()
    {
        // Arrange
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        var hundredDollars = new Money(100.00m, Currencies.Default);
        var threePercentFee = new Money(-3.00m, Currencies.Default);
        var fiftyDollars = new Money(50.00m, Currencies.Default);
        
        // Act - Simulate a month of restaurant operations
        
        // Week 1: Receive orders
        account.AddOrderRevenue(hundredDollars, DefaultOrderId);
        account.AddPlatformFee(threePercentFee, DefaultOrderId);
        
        // Week 2: More orders and a refund
        account.AddOrderRevenue(fiftyDollars, DefaultOrderId);
        account.AddRefundDeduction(new Money(-10.00m, Currencies.Default), DefaultOrderId);
        
        // Week 3: Manual adjustment for promotion reimbursement
        account.AddManualAdjustment(new Money(5.00m, Currencies.Default));
        
        // Week 4: Process payout
        var payoutAmount = new Money(100.00m, Currencies.Default);
        var payoutResult = account.ProcessPayout(payoutAmount);
        
        // Assert
        payoutResult.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(42.00m); // 100 - 3 + 50 - 10 + 5 - 100 = 42
        account.Transactions.Should().HaveCount(6);
        
        // Verify event history
        var transactionEvents = account.DomainEvents.OfType<TransactionAdded>();
        transactionEvents.Should().HaveCount(6);
        
        var payoutEvents = account.DomainEvents.OfType<PayoutSettled>();
        payoutEvents.Should().HaveCount(1);
        payoutEvents.Single().PayoutAmount.Should().Be(payoutAmount);
    }

    #endregion

    #region Helper Methods

    private static RestaurantAccount CreateAccountWithBalance(decimal balance)
    {
        var account = RestaurantAccount.Create(DefaultRestaurantId).ValueOrFail();
        if (balance > 0)
        {
            var amount = new Money(balance, Currencies.Default);
            account.AddOrderRevenue(amount, DefaultOrderId);
        }
        account.ClearDomainEvents();
        return account;
    }

    #endregion
}
