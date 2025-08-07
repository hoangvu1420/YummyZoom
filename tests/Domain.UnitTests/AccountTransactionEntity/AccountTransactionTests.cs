using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Domain.AccountTransactionEntity.Enums;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.AccountTransactionEntity;

[TestFixture]
public class AccountTransactionTests
{
    private static readonly RestaurantAccountId DefaultRestaurantAccountId = RestaurantAccountId.CreateUnique();
    private static readonly Money TenDollars = new Money(10.00m, Currencies.Default);
    private static readonly Money NegativeTenDollars = new Money(-10.00m, Currencies.Default);
    private static readonly OrderId DefaultOrderId = OrderId.Create(Guid.NewGuid());

    #region Create() Method Tests

    [Test]
    public void Create_WithOrderRevenue_ShouldSucceed()
    {
        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.OrderRevenue,
            TenDollars,
            DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        var transaction = result.Value;
        transaction.Should().NotBeNull();
        transaction.RestaurantAccountId.Should().Be(DefaultRestaurantAccountId);
        transaction.Type.Should().Be(TransactionType.OrderRevenue);
        transaction.Amount.Should().Be(TenDollars);
        transaction.RelatedOrderId.Should().Be(DefaultOrderId);
        transaction.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_WithPlatformFee_ShouldSucceed()
    {
        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.PlatformFee,
            NegativeTenDollars,
            DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Type.Should().Be(TransactionType.PlatformFee);
        result.Value.Amount.Should().Be(NegativeTenDollars);
    }

    [Test]
    public void Create_WithManualAdjustment_ShouldSucceed()
    {
        // Arrange
        var notes = "Manual adjustment for testing";

        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.ManualAdjustment,
            TenDollars,
            notes: notes);

        // Assert
        result.ShouldBeSuccessful();
        var transaction = result.Value;
        transaction.Type.Should().Be(TransactionType.ManualAdjustment);
        transaction.Notes.Should().Be(notes);
        transaction.RelatedOrderId.Should().BeNull();
    }

    [Test]
    public void Create_WithInvalidOrderRevenue_ShouldFail()
    {
        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.OrderRevenue,
            NegativeTenDollars, // Invalid amount
            DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.OrderRevenueMustBePositive(NegativeTenDollars));
    }

    [Test]
    public void Create_WithInvalidPlatformFee_ShouldFail()
    {
        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.PlatformFee,
            TenDollars, // Invalid amount
            DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.PlatformFeeMustBeNegative(TenDollars));
    }

    [Test]
    public void Create_WithInvalidRefundDeduction_ShouldFail()
    {
        // Act
        var result = AccountTransaction.Create(
            DefaultRestaurantAccountId,
            TransactionType.RefundDeduction,
            TenDollars, // Invalid amount
            DefaultOrderId);

        // Assert
        result.ShouldBeSuccessful();
        result.Error.Should().Be(RestaurantAccountErrors.RefundDeductionMustBeNegative(TenDollars));
    }

    #endregion
}
