using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.UserAggregate.Entities;

[TestFixture]
public class PaymentMethodTests
{
    [Test]
    public void Create_WithValidInputs_ShouldReturnPaymentMethod()
    {
        // Arrange
        var type = "Card";
        var tokenizedDetails = "tok_test";
        var isDefault = false;

        // Act
        var paymentMethod = PaymentMethod.Create(type, tokenizedDetails, isDefault);

        // Assert
        paymentMethod.Should().NotBeNull();
        paymentMethod.Id.Should().NotBeNull();
        paymentMethod.Id.Value.Should().NotBe(Guid.Empty); 
        paymentMethod.Type.Should().Be(type);
        paymentMethod.TokenizedDetails.Should().Be(tokenizedDetails);
        paymentMethod.IsDefault.Should().Be(isDefault);
    }

    [Test]
    public void Equality_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var paymentMethodId = PaymentMethodId.CreateUnique();
        var paymentMethod1 = PaymentMethod.Create(paymentMethodId, "Card", "tok_test", false); 
        var paymentMethod2 = PaymentMethod.Create(paymentMethodId, "PayPal", "tok_other", true); 

        // Assert
        paymentMethod1.Should().Be(paymentMethod2);
        (paymentMethod1 == paymentMethod2).Should().BeTrue();
        paymentMethod1.GetHashCode().Should().Be(paymentMethod2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var paymentMethod1 = PaymentMethod.Create("Card", "tok_test", false);
        var paymentMethod2 = PaymentMethod.Create("Card", "tok_test", false); // Different IDs

        // Assert
        paymentMethod1.Should().NotBe(paymentMethod2);
        (paymentMethod1 != paymentMethod2).Should().BeTrue();
    }

    [Test]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        // Arrange
        var paymentMethod = PaymentMethod.Create("Card", "tok_test", false);
        paymentMethod.IsDefault.Should().BeFalse();

        // Act
        paymentMethod.SetAsDefault();

        // Assert
        paymentMethod.IsDefault.Should().BeTrue();
    }
}
