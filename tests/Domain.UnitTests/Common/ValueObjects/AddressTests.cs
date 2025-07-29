using YummyZoom.Domain.Common.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Common.ValueObjects;

[TestFixture]
public class AddressTests
{
    [Test]
    public void Create_WithValidInputs_ShouldReturnAddress()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";
        var label = "Home";
        var deliveryInstructions = "Leave at the door";

        // Act
        var address = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
        address.Country.Should().Be(country);
        address.Label.Should().Be(label);
        address.DeliveryInstructions.Should().Be(deliveryInstructions);
    }

    [Test]
    public void Create_WithOnlyRequiredInputs_ShouldReturnAddressWithNullOptionals()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";

        // Act
        var address = Address.Create(street, city, state, zipCode, country);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
        address.Country.Should().Be(country);
        address.Label.Should().BeNull();
        address.DeliveryInstructions.Should().BeNull();
    }

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "91234";
        var country = "USA";
        var label = "Home";
        var deliveryInstructions = "Leave at the door";

        var address1 = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);
        var address2 = Address.Create(street, city, state, zipCode, country, label, deliveryInstructions);

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address2 = Address.Create("456 Oak Ave", "Otherville", "NY", "56789", "USA", "Work", "Ring the bell"); // Different values

        var address3 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address4 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Work", "Leave at the door"); // Different label

        var address5 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Leave at the door");
        var address6 = Address.Create("123 Main St", "Anytown", "CA", "91234", "USA", "Home", "Ring the bell"); // Different delivery instructions


        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();

        address3.Should().NotBe(address4);
        (address3 == address4).Should().BeFalse();

        address5.Should().NotBe(address6);
        (address5 == address6).Should().BeFalse();
    }
}
