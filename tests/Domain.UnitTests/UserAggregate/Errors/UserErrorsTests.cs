using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.UserAggregate.Errors;

[TestFixture]
public class UserErrorsTests
{
    [Test]
    public void InvalidUserId_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var invalidValue = "some-invalid-id";

        // Act
        var error = UserErrors.InvalidUserId(invalidValue);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.InvalidUserId");
        error.Description.Should().Contain(invalidValue);
    }

    [Test]
    public void RoleNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var roleName = "NonExistentRole";

        // Act
        var error = UserErrors.RoleNotFound(roleName);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.RoleNotFound");
        error.Description.Should().Contain(roleName);
    }

    [Test]
    public void CannotRemoveLastRole_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.CannotRemoveLastRole;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.CannotRemoveLastRole");
        error.Description.Should().Be("Cannot remove the last role from the user.");
    }

    [Test]
    public void AddressNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var addressId = Guid.NewGuid();

        // Act
        var error = UserErrors.AddressNotFound(addressId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.AddressNotFound");
        error.Description.Should().Contain(addressId.ToString());
    }

    [Test]
    public void PaymentMethodNotFound_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var paymentMethodId = Guid.NewGuid();

        // Act
        var error = UserErrors.PaymentMethodNotFound(paymentMethodId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.PaymentMethodNotFound");
        error.Description.Should().Contain(paymentMethodId.ToString());
    }

    [Test]
    public void InvalidPaymentMethod_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.InvalidPaymentMethod;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.InvalidPaymentMethod");
        error.Description.Should().Be("Payment method is invalid.");
    }

     [Test]
    public void MustHaveAtLeastOneRole_ShouldReturnErrorWithCorrectCodeAndMessage()
    {
        // Act
        var error = UserErrors.MustHaveAtLeastOneRole;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.MustHaveAtLeastOneRole");
        error.Description.Should().Be("User must have at least one role.");
    }
}
