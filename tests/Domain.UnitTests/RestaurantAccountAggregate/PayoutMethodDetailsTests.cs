using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAccountAggregate;

[TestFixture]
public class PayoutMethodDetailsTests
{
    private const string ValidPayoutMethod = "Bank Account: ****1234";
    private const string PayoutMethodWithWhitespace = "  PayPal: user@example.com  ";
    private const string TrimmedPayoutMethod = "PayPal: user@example.com";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidDetails_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = PayoutMethodDetails.Create(ValidPayoutMethod);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payoutMethod = result.Value;
        payoutMethod.Details.Should().Be(ValidPayoutMethod);
    }

    [Test]
    public void Create_WithDetailsContainingWhitespace_ShouldTrimWhitespace()
    {
        // Arrange & Act
        var result = PayoutMethodDetails.Create(PayoutMethodWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Details.Should().Be(TrimmedPayoutMethod);
    }

    [Test]
    public void Create_WithNullDetails_ShouldFailWithInvalidPayoutMethodError()
    {
        // Arrange & Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var result = PayoutMethodDetails.Create(null);
#pragma warning restore CS8625

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantAccountErrors.InvalidPayoutMethod);
    }

    [Test]
    public void Create_WithEmptyDetails_ShouldFailWithInvalidPayoutMethodError()
    {
        // Arrange & Act
        var result = PayoutMethodDetails.Create(string.Empty);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantAccountErrors.InvalidPayoutMethod);
    }

    [Test]
    public void Create_WithWhitespaceOnlyDetails_ShouldFailWithInvalidPayoutMethodError()
    {
        // Arrange & Act
        var result = PayoutMethodDetails.Create("   ");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(RestaurantAccountErrors.InvalidPayoutMethod);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameDetails_ShouldReturnTrue()
    {
        // Arrange
        var payoutMethod1 = PayoutMethodDetails.Create(ValidPayoutMethod).Value;
        var payoutMethod2 = PayoutMethodDetails.Create(ValidPayoutMethod).Value;

        // Act & Assert
        payoutMethod1.Equals(payoutMethod2).Should().BeTrue();
        (payoutMethod1 == payoutMethod2).Should().BeTrue();
        payoutMethod1.GetHashCode().Should().Be(payoutMethod2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentDetails_ShouldReturnFalse()
    {
        // Arrange
        var payoutMethod1 = PayoutMethodDetails.Create("Bank Account: ****1234").Value;
        var payoutMethod2 = PayoutMethodDetails.Create("PayPal: user@example.com").Value;

        // Act & Assert
        payoutMethod1.Equals(payoutMethod2).Should().BeFalse();
        (payoutMethod1 == payoutMethod2).Should().BeFalse();
        (payoutMethod1 != payoutMethod2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var payoutMethod = PayoutMethodDetails.Create(ValidPayoutMethod).Value;

        // Act & Assert
        payoutMethod.Equals(null).Should().BeFalse();
#pragma warning disable CS8625, CS8604
        (payoutMethod == null).Should().BeFalse();
        (payoutMethod != null).Should().BeTrue();
#pragma warning restore CS8625, CS8604
    }

    [Test]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var payoutMethod = PayoutMethodDetails.Create(ValidPayoutMethod).Value;
        var differentObject = "Some string";

        // Act & Assert
        payoutMethod.Equals(differentObject).Should().BeFalse();
    }

    [Test]
    public void Equals_CaseInsensitive_ShouldReturnFalse()
    {
        // Arrange
        var payoutMethod1 = PayoutMethodDetails.Create("Bank Account").Value;
        var payoutMethod2 = PayoutMethodDetails.Create("BANK ACCOUNT").Value;

        // Act & Assert
        // Value objects should be case-sensitive by default
        payoutMethod1.Equals(payoutMethod2).Should().BeFalse();
    }

    #endregion

    #region Real-world Scenarios

    [TestCase("Bank Account: ****1234")]
    [TestCase("PayPal: user@example.com")]
    [TestCase("Stripe Connect: acct_1234567890")]
    [TestCase("Wire Transfer: Routing 123456789, Account ****7890")]
    [TestCase("Crypto Wallet: 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa")]
    public void Create_WithVariousValidPayoutMethods_ShouldSucceed(string payoutMethodDetails)
    {
        // Arrange & Act
        var result = PayoutMethodDetails.Create(payoutMethodDetails);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Details.Should().Be(payoutMethodDetails);
    }

    #endregion
}
