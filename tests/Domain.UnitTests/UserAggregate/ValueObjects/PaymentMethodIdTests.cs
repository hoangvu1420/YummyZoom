using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors; // Assuming InvalidPaymentMethod error is here

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class PaymentMethodIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnPaymentMethodIdWithNonEmptyGuidValue()
    {
        // Act
        var paymentMethodId = PaymentMethodId.CreateUnique();

        // Assert
        paymentMethodId.Should().NotBeNull();
        paymentMethodId.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Create_WithValidGuid_ShouldReturnPaymentMethodIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var paymentMethodId = PaymentMethodId.Create(guid);

        // Assert
        paymentMethodId.Should().NotBeNull();
        paymentMethodId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithValidGuidString_ShouldReturnSuccessResultWithPaymentMethodId()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = PaymentMethodId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.ToString().Should().Be(guidString);
    }

    [Test]
    public void Create_WithInvalidGuidString_ShouldReturnFailureResultWithInvalidPaymentMethodError()
    {
        // Arrange
        var invalidGuidString = "invalid-guid-string";

        // Act
        var result = PaymentMethodId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Assuming UserErrors.InvalidPaymentMethod is used for invalid GUID string
        result.Error.Should().Be(UserErrors.InvalidPaymentMethod);
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var paymentMethodId1 = PaymentMethodId.Create(guid);
        var paymentMethodId2 = PaymentMethodId.Create(guid);

        // Assert
        paymentMethodId1.Should().Be(paymentMethodId2);
        (paymentMethodId1 == paymentMethodId2).Should().BeTrue();
        paymentMethodId1.GetHashCode().Should().Be(paymentMethodId2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var paymentMethodId1 = PaymentMethodId.CreateUnique();
        var paymentMethodId2 = PaymentMethodId.CreateUnique();

        // Assert
        paymentMethodId1.Should().NotBe(paymentMethodId2);
        (paymentMethodId1 != paymentMethodId2).Should().BeTrue();
    }
}
