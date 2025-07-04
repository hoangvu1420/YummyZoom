using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate.ValueObjects;

/// <summary>
/// Tests for Address value object validation and creation.
/// </summary>
[TestFixture]
public class AddressTests
{
    private const string ValidStreet = "123 Main St";
    private const string ValidCity = "Test City";
    private const string ValidState = "Test State";
    private const string ValidZipCode = "12345";
    private const string ValidCountry = "Test Country";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndCreateAddress()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var address = result.Value;
        
        address.Street.Should().Be(ValidStreet);
        address.City.Should().Be(ValidCity);
        address.State.Should().Be(ValidState);
        address.ZipCode.Should().Be(ValidZipCode);
        address.Country.Should().Be(ValidCountry);
    }

    [Test]
    public void Create_WithWhitespaceInputs_ShouldTrimAndSucceed()
    {
        // Arrange & Act
        var result = Address.Create(" 123 Main St ", " Test City ", " Test State ", " 12345 ", " Test Country ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var address = result.Value;
        
        address.Street.Should().Be(ValidStreet);
        address.City.Should().Be(ValidCity);
        address.State.Should().Be(ValidState);
        address.ZipCode.Should().Be(ValidZipCode);
        address.Country.Should().Be(ValidCountry);
    }

    #endregion

    #region Street Validation Tests

    [Test]
    public void Create_WithNullStreet_ShouldFailWithStreetRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(null!, ValidCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStreetIsRequired());
    }

    [Test]
    public void Create_WithEmptyStreet_ShouldFailWithStreetRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(string.Empty, ValidCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStreetIsRequired());
    }

    [Test]
    public void Create_WithWhitespaceOnlyStreet_ShouldFailWithStreetRequiredError()
    {
        // Arrange & Act
        var result = Address.Create("   ", ValidCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStreetIsRequired());
    }

    [Test]
    public void Create_WithTooLongStreet_ShouldFailWithFieldTooLongError()
    {
        // Arrange
        var tooLongStreet = new string('a', 101); // Max is 100

        // Act
        var result = Address.Create(tooLongStreet, ValidCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressFieldTooLong("Street", 100));
    }

    #endregion

    #region City Validation Tests

    [Test]
    public void Create_WithNullCity_ShouldFailWithCityRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, null!, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressCityIsRequired());
    }

    [Test]
    public void Create_WithEmptyCity_ShouldFailWithCityRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, string.Empty, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressCityIsRequired());
    }

    [Test]
    public void Create_WithTooLongCity_ShouldFailWithFieldTooLongError()
    {
        // Arrange
        var tooLongCity = new string('a', 101); // Max is 100

        // Act
        var result = Address.Create(ValidStreet, tooLongCity, ValidState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressFieldTooLong("City", 100));
    }

    #endregion

    #region State Validation Tests

    [Test]
    public void Create_WithNullState_ShouldFailWithStateRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, null!, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStateIsRequired());
    }

    [Test]
    public void Create_WithEmptyState_ShouldFailWithStateRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, string.Empty, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStateIsRequired());
    }

    [Test]
    public void Create_WithTooLongState_ShouldFailWithFieldTooLongError()
    {
        // Arrange
        var tooLongState = new string('a', 101); // Max is 100

        // Act
        var result = Address.Create(ValidStreet, ValidCity, tooLongState, ValidZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressFieldTooLong("State", 100));
    }

    #endregion

    #region ZipCode Validation Tests

    [Test]
    public void Create_WithNullZipCode_ShouldFailWithZipCodeRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, null!, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressZipCodeIsRequired());
    }

    [Test]
    public void Create_WithEmptyZipCode_ShouldFailWithZipCodeRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, string.Empty, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressZipCodeIsRequired());
    }

    [Test]
    public void Create_WithTooLongZipCode_ShouldFailWithFieldTooLongError()
    {
        // Arrange
        var tooLongZipCode = new string('1', 21); // Max is 20

        // Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, tooLongZipCode, ValidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressFieldTooLong("ZipCode", 20));
    }

    #endregion

    #region Country Validation Tests

    [Test]
    public void Create_WithNullCountry_ShouldFailWithCountryRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressCountryIsRequired());
    }

    [Test]
    public void Create_WithEmptyCountry_ShouldFailWithCountryRequiredError()
    {
        // Arrange & Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressCountryIsRequired());
    }

    [Test]
    public void Create_WithTooLongCountry_ShouldFailWithFieldTooLongError()
    {
        // Arrange
        var tooLongCountry = new string('a', 101); // Max is 100

        // Act
        var result = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, tooLongCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressFieldTooLong("Country", 100));
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, ValidCountry).Value;
        var address2 = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, ValidCountry).Value;

        // Act & Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create(ValidStreet, ValidCity, ValidState, ValidZipCode, ValidCountry).Value;
        var address2 = Address.Create("456 Oak Ave", ValidCity, ValidState, ValidZipCode, ValidCountry).Value;

        // Act & Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    #endregion
}
