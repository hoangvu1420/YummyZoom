using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate.ValueObjects;

[TestFixture]
public class DeliveryAddressTests
{
    private const string DefaultStreet = "123 Main Street";
    private const string DefaultCity = "Springfield";
    private const string DefaultState = "Illinois";
    private const string DefaultZipCode = "62701";
    private const string DefaultCountry = "USA";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var address = result.Value;
        
        address.Street.Should().Be(DefaultStreet);
        address.City.Should().Be(DefaultCity);
        address.State.Should().Be(DefaultState);
        address.ZipCode.Should().Be(DefaultZipCode);
        address.Country.Should().Be(DefaultCountry);
    }

    [Test]
    public void Create_WithMinimalValidInputs_ShouldSucceed()
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            "1 A St",
            "B",
            "C",
            "1",
            "D");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var address = result.Value;
        address.Street.Should().Be("1 A St");
        address.City.Should().Be("B");
        address.State.Should().Be("C");
        address.ZipCode.Should().Be("1");
        address.Country.Should().Be("D");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyStreet_ShouldFailWithAddressInvalidError(string invalidStreet)
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            invalidStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [Test]
    public void Create_WithNullStreet_ShouldFailWithAddressInvalidError()
    {
        // Arrange & Act
#pragma warning disable CS8625
        var result = DeliveryAddress.Create(
            null,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyCity_ShouldFailWithAddressInvalidError(string invalidCity)
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            DefaultStreet,
            invalidCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyState_ShouldFailWithAddressInvalidError(string invalidState)
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            invalidState,
            DefaultZipCode,
            DefaultCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyZipCode_ShouldFailWithAddressInvalidError(string invalidZipCode)
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            invalidZipCode,
            DefaultCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithEmptyCountry_ShouldFailWithAddressInvalidError(string invalidCountry)
    {
        // Arrange & Act
        var result = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            invalidCountry);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    [Test]
    public void Create_WithAllFieldsEmpty_ShouldFailWithAddressInvalidError()
    {
        // Arrange & Act
        var result = DeliveryAddress.Create("", "", "", "", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.AddressInvalid);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        var address2 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        // Act & Assert
        address1.Should().Be(address2);
        address1.Equals(address2).Should().BeTrue();
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
    }

    [Test]
    public void Equality_WithDifferentStreets_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            "123 Main St",
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        var address2 = DeliveryAddress.Create(
            "456 Oak Ave",
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        // Act & Assert
        address1.Should().NotBe(address2);
        address1.Equals(address2).Should().BeFalse();
        (address1 == address2).Should().BeFalse();
        (address1 != address2).Should().BeTrue();
    }

    [Test]
    public void Equality_WithDifferentCities_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            DefaultStreet,
            "Springfield",
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        var address2 = DeliveryAddress.Create(
            DefaultStreet,
            "Chicago",
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Test]
    public void Equality_WithDifferentStates_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            "Illinois",
            DefaultZipCode,
            DefaultCountry).Value;

        var address2 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            "California",
            DefaultZipCode,
            DefaultCountry).Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Test]
    public void Equality_WithDifferentZipCodes_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            "62701",
            DefaultCountry).Value;

        var address2 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            "90210",
            DefaultCountry).Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Test]
    public void Equality_WithDifferentCountries_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            "USA").Value;

        var address2 = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            "Canada").Value;

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Test]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var address = DeliveryAddress.Create(
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry).Value;

        // Act & Assert
        address.Equals(null).Should().BeFalse();
        (address is null).Should().BeFalse();
    }

    #endregion
}
