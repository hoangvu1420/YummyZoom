using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Entities;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAccountAggregate;

[TestFixture]
public class AccountTransactionTests
{
    private static readonly Money TenDollars = new Money(10.00m, Currencies.Default);
    private static readonly Money NegativeFiveDollars = new Money(-5.00m, Currencies.Default);
    private static readonly Money ZeroDollars = new Money(0.00m, Currencies.Default);
    private static readonly OrderId DefaultOrderId = OrderId.Create(Guid.NewGuid());

    #region Create() Method Tests - OrderRevenue

    [Test]
    public void Create_OrderRevenueWithPositiveAmount_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.OrderRevenue, TenDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Id.Value.Should().NotBe(Guid.Empty);
        transaction.Type.Should().Be(TransactionType.OrderRevenue);
        transaction.Amount.Should().Be(TenDollars);
        transaction.RelatedOrderId.Should().Be(DefaultOrderId);
        transaction.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_OrderRevenueWithNegativeAmount_ShouldFailWithOrderRevenueMustBePositiveError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.OrderRevenue, NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive);
    }

    [Test]
    public void Create_OrderRevenueWithZeroAmount_ShouldFailWithOrderRevenueMustBePositiveError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.OrderRevenue, ZeroDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive);
    }

    #endregion

    #region Create() Method Tests - PlatformFee

    [Test]
    public void Create_PlatformFeeWithNegativeAmount_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.PlatformFee, NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.PlatformFee);
        transaction.Amount.Should().Be(NegativeFiveDollars);
        transaction.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void Create_PlatformFeeWithPositiveAmount_ShouldFailWithPlatformFeeMustBeNegativeError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.PlatformFee, TenDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.PlatformFeeMustBeNegative);
    }

    [Test]
    public void Create_PlatformFeeWithZeroAmount_ShouldFailWithPlatformFeeMustBeNegativeError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.PlatformFee, ZeroDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.PlatformFeeMustBeNegative);
    }

    #endregion

    #region Create() Method Tests - RefundDeduction

    [Test]
    public void Create_RefundDeductionWithNegativeAmount_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.RefundDeduction, NegativeFiveDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.RefundDeduction);
        transaction.Amount.Should().Be(NegativeFiveDollars);
        transaction.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    [Test]
    public void Create_RefundDeductionWithPositiveAmount_ShouldFailWithRefundDeductionMustBeNegativeError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.RefundDeduction, TenDollars, DefaultOrderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.RefundDeductionMustBeNegative);
    }

    #endregion

    #region Create() Method Tests - PayoutSettlement

    [Test]
    public void Create_PayoutSettlementWithNegativeAmount_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.PayoutSettlement, NegativeFiveDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.PayoutSettlement);
        transaction.Amount.Should().Be(NegativeFiveDollars);
        transaction.RelatedOrderId.Should().BeNull(); // Payouts typically don't have related orders
    }

    [Test]
    public void Create_PayoutSettlementWithPositiveAmount_ShouldFailWithPayoutSettlementMustBeNegativeError()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.PayoutSettlement, TenDollars);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantAccountErrors.PayoutSettlementMustBeNegative);
    }

    #endregion

    #region Create() Method Tests - ManualAdjustment

    [Test]
    public void Create_ManualAdjustmentWithPositiveAmount_ShouldSucceed()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.ManualAdjustment, TenDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.ManualAdjustment);
        transaction.Amount.Should().Be(TenDollars);
        transaction.RelatedOrderId.Should().BeNull();
    }

    [Test]
    public void Create_ManualAdjustmentWithNegativeAmount_ShouldSucceed()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.ManualAdjustment, NegativeFiveDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.ManualAdjustment);
        transaction.Amount.Should().Be(NegativeFiveDollars);
    }

    [Test]
    public void Create_ManualAdjustmentWithZeroAmount_ShouldSucceed()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.ManualAdjustment, ZeroDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var transaction = result.Value;
        transaction.Amount.Should().Be(ZeroDollars);
    }

    #endregion

    #region Optional Parameters Tests

    [Test]
    public void Create_WithoutOrderId_ShouldSucceedAndSetRelatedOrderIdToNull()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.ManualAdjustment, TenDollars);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RelatedOrderId.Should().BeNull();
    }

    [Test]
    public void Create_WithOrderId_ShouldSucceedAndSetRelatedOrderIdCorrectly()
    {
        // Arrange & Act
        var result = AccountTransaction.Create(TransactionType.OrderRevenue, TenDollars, DefaultOrderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RelatedOrderId.Should().Be(DefaultOrderId);
    }

    #endregion

    #region Transaction Type Validation Edge Cases

    [TestCase(TransactionType.OrderRevenue)]
    [TestCase(TransactionType.PlatformFee)]
    [TestCase(TransactionType.RefundDeduction)]
    [TestCase(TransactionType.PayoutSettlement)]
    [TestCase(TransactionType.ManualAdjustment)]
    public void Create_AllTransactionTypes_ShouldSetTimestampToCurrentUtcTime(TransactionType transactionType)
    {
        // Arrange
        var amount = transactionType == TransactionType.OrderRevenue ? TenDollars : 
                    transactionType == TransactionType.ManualAdjustment ? TenDollars : NegativeFiveDollars;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = AccountTransaction.Create(transactionType, amount);

        // Assert
        if (result.IsSuccess)
        {
            var afterCreation = DateTime.UtcNow;
            result.Value.Timestamp.Should().BeOnOrAfter(beforeCreation);
            result.Value.Timestamp.Should().BeOnOrBefore(afterCreation);
            result.Value.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        }
    }

    #endregion
}
