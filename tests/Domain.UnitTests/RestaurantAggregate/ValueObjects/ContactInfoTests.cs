using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate.ValueObjects;

/// <summary>
/// Tests for ContactInfo value object creation, validation, and equality.
/// </summary>
[TestFixture]
public class ContactInfoTests
{
    private const string ValidPhoneNumber = "123-456-7890";
    private const string ValidEmail = "test@example.com";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeContactInfoCorrectly()
    {
        // Arrange & Act
        var result = ContactInfo.Create(ValidPhoneNumber, ValidEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var contactInfo = result.Value;
        
        contactInfo.Should().NotBeNull();
        contactInfo.PhoneNumber.Should().Be(ValidPhoneNumber);
        contactInfo.Email.Should().Be(ValidEmail);
    }

    [Test]
    public void Create_WithNullOrEmptyPhoneNumber_ShouldFailWithPhoneNumberRequiredError()
    {
        // Arrange & Act
        var result = ContactInfo.Create(string.Empty, ValidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactPhoneIsRequired());
    }

    [Test]
    public void Create_WithNullPhoneNumber_ShouldFailWithPhoneNumberRequiredError()
    {
        // Arrange & Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var result = ContactInfo.Create(null, ValidEmail);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactPhoneIsRequired());
    }

    [Test]
    public void Create_WithNullOrEmptyEmail_ShouldFailWithEmailRequiredError()
    {
        // Arrange & Act
        var result = ContactInfo.Create(ValidPhoneNumber, string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailIsRequired());
    }

    [Test]
    public void Create_WithNullEmail_ShouldFailWithEmailRequiredError()
    {
        // Arrange & Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var result = ContactInfo.Create(ValidPhoneNumber, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailIsRequired());
    }

    [Test]
    public void Create_WithInvalidEmailFormat_ShouldFailWithInvalidEmailFormatError()
    {
        // Arrange & Act
        var result = ContactInfo.Create(ValidPhoneNumber, "invalid-email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailInvalidFormat("invalid-email"));
    }

    [Test]
    public void Create_WithWhitespaceInputs_ShouldTrimAndSucceed()
    {
        // Arrange
        var phoneWithWhitespace = "  " + ValidPhoneNumber + "  ";
        var emailWithWhitespace = "  " + ValidEmail + "  ";

        // Act
        var result = ContactInfo.Create(phoneWithWhitespace, emailWithWhitespace);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var contactInfo = result.Value;
        
        contactInfo.PhoneNumber.Should().Be(ValidPhoneNumber);
        contactInfo.Email.Should().Be(ValidEmail);
    }

    [Test]
    [TestCase("test@example.com")]
    [TestCase("user.name@domain.co.uk")]
    [TestCase("user+tag@example.org")]
    [TestCase("123@456.com")]
    public void Create_WithValidEmailFormats_ShouldSucceed(string email)
    {
        // Arrange & Act
        var result = ContactInfo.Create(ValidPhoneNumber, email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
    }

    [Test]
    [TestCase("plaintext")]
    [TestCase("@domain.com")]
    [TestCase("user@")]
    [TestCase("user@domain")]
    public void Create_WithInvalidEmailFormats_ShouldFailWithInvalidEmailFormatError(string email)
    {
        // Arrange & Act
        var result = ContactInfo.Create(ValidPhoneNumber, email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailInvalidFormat(email));
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var contactInfo1 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;
        var contactInfo2 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;

        // Act & Assert
        contactInfo1.Should().Be(contactInfo2);
        contactInfo1.Equals(contactInfo2).Should().BeTrue();
        (contactInfo1 == contactInfo2).Should().BeTrue();
        (contactInfo1 != contactInfo2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentPhoneNumbers_ShouldReturnFalse()
    {
        // Arrange
        var contactInfo1 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;
        var contactInfo2 = ContactInfo.Create("987-654-3210", ValidEmail).Value;

        // Act & Assert
        contactInfo1.Should().NotBe(contactInfo2);
        contactInfo1.Equals(contactInfo2).Should().BeFalse();
        (contactInfo1 == contactInfo2).Should().BeFalse();
        (contactInfo1 != contactInfo2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithDifferentEmails_ShouldReturnFalse()
    {
        // Arrange
        var contactInfo1 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;
        var contactInfo2 = ContactInfo.Create(ValidPhoneNumber, "different@example.com").Value;

        // Act & Assert
        contactInfo1.Should().NotBe(contactInfo2);
        contactInfo1.Equals(contactInfo2).Should().BeFalse();
        (contactInfo1 == contactInfo2).Should().BeFalse();
        (contactInfo1 != contactInfo2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var contactInfo = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;

        // Act & Assert
        contactInfo.Equals(null).Should().BeFalse();
        (contactInfo is null).Should().BeFalse();
        (contactInfo is not null).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var contactInfo1 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;
        var contactInfo2 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;

        // Act & Assert
        contactInfo1.GetHashCode().Should().Be(contactInfo2.GetHashCode());
    }

    [Test]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var contactInfo1 = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;
        var contactInfo2 = ContactInfo.Create("987-654-3210", "different@example.com").Value;

        // Act & Assert
        contactInfo1.GetHashCode().Should().NotBe(contactInfo2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Test]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var contactInfo = ContactInfo.Create(ValidPhoneNumber, ValidEmail).Value;

        // Act
        var result = contactInfo.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion
}
