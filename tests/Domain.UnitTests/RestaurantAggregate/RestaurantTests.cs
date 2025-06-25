
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate;

[TestFixture]
public class RestaurantTests
{
    private const string DefaultName = "Test Restaurant";
    private const string DefaultLogoUrl = "http://example.com/logo.png";
    private const string DefaultDescription = "Test Description";
    private const string DefaultCuisineType = "Test Cuisine";

    private static Address CreateValidAddress() => Address.Create("123 Main St", "Test City", "Test State", "12345", "Test Country");
    private static ContactInfo CreateValidContactInfo() => ContactInfo.Create("123-456-7890", "test@example.com");
    private static BusinessHours CreateValidBusinessHours() => BusinessHours.Create("Mon-Fri: 9am-5pm");

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeRestaurantCorrectly()
    {
        // Arrange & Act
        var restaurant = Restaurant.Create(DefaultName, DefaultLogoUrl, DefaultDescription, DefaultCuisineType, CreateValidAddress(), CreateValidContactInfo(), CreateValidBusinessHours());

        // Assert
        restaurant.Should().NotBeNull();
        restaurant.Id.Value.Should().NotBe(Guid.Empty);
        restaurant.Name.Should().Be(DefaultName);
        restaurant.LogoUrl.Should().Be(DefaultLogoUrl);
        restaurant.Description.Should().Be(DefaultDescription);
        restaurant.CuisineType.Should().Be(DefaultCuisineType);
        restaurant.Location.Should().Be(CreateValidAddress());
        restaurant.ContactInfo.Should().Be(CreateValidContactInfo());
        restaurant.BusinessHours.Should().Be(CreateValidBusinessHours());
        restaurant.IsVerified.Should().BeFalse();
        restaurant.IsAcceptingOrders.Should().BeFalse();
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantCreated));
    }

    [Test]
    public void UpdateDetails_Should_UpdateRestaurantProperties_And_RaiseRestaurantUpdatedEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var newName = "New Test Restaurant";
        var newDescription = "New Test Description";
        var newCuisineType = "New Test Cuisine";
        var newLogoUrl = "http://example.com/new-logo.png";
        var newLocation = Address.Create("456 Oak Ave", "New Test City", "New Test State", "67890", "New Test Country");
        var newContactInfo = ContactInfo.Create("098-765-4321", "new-test@example.com");
        var newBusinessHours = BusinessHours.Create("Sat-Sun: 10am-4pm");

        // Act
        restaurant.UpdateDetails(newName, newDescription, newCuisineType, newLogoUrl, newLocation, newContactInfo, newBusinessHours);

        // Assert
        restaurant.Name.Should().Be(newName);
        restaurant.Description.Should().Be(newDescription);
        restaurant.CuisineType.Should().Be(newCuisineType);
        restaurant.LogoUrl.Should().Be(newLogoUrl);
        restaurant.Location.Should().Be(newLocation);
        restaurant.ContactInfo.Should().Be(newContactInfo);
        restaurant.BusinessHours.Should().Be(newBusinessHours);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantUpdated));
    }

    [Test]
    public void Verify_Should_SetIsVerifiedToTrue_And_RaiseRestaurantVerifiedEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        restaurant.Verify();

        // Assert
        restaurant.IsVerified.Should().BeTrue();
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantVerified));
    }

    [Test]
    public void Verify_Should_DoNothing_When_AlreadyVerified()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        restaurant.Verify();
        var domainEventCount = restaurant.DomainEvents.Count;

        // Act
        restaurant.Verify();

        // Assert
        restaurant.IsVerified.Should().BeTrue();
        restaurant.DomainEvents.Should().HaveCount(domainEventCount);
    }

    [Test]
    public void AcceptOrders_Should_SetIsAcceptingOrdersToTrue_And_RaiseRestaurantAcceptingOrdersEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        restaurant.AcceptOrders();

        // Assert
        restaurant.IsAcceptingOrders.Should().BeTrue();
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantAcceptingOrders));
    }

    [Test]
    public void AcceptOrders_Should_DoNothing_When_AlreadyAcceptingOrders()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        restaurant.AcceptOrders();
        var domainEventCount = restaurant.DomainEvents.Count;

        // Act
        restaurant.AcceptOrders();

        // Assert
        restaurant.IsAcceptingOrders.Should().BeTrue();
        restaurant.DomainEvents.Should().HaveCount(domainEventCount);
    }

    [Test]
    public void DeclineOrders_Should_SetIsAcceptingOrdersToFalse_And_RaiseRestaurantNotAcceptingOrdersEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        restaurant.AcceptOrders();

        // Act
        restaurant.DeclineOrders();

        // Assert
        restaurant.IsAcceptingOrders.Should().BeFalse();
        restaurant.DomainEvents.Should().Contain(e => e.GetType() == typeof(RestaurantNotAcceptingOrders));
    }

    [Test]
    public void DeclineOrders_Should_DoNothing_When_NotAcceptingOrders()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var domainEventCount = restaurant.DomainEvents.Count;

        // Act
        restaurant.DeclineOrders();

        // Assert
        restaurant.IsAcceptingOrders.Should().BeFalse();
        restaurant.DomainEvents.Should().HaveCount(domainEventCount);
    }

    private Restaurant CreateTestRestaurant()
    {
        return Restaurant.Create(
            DefaultName,
            DefaultLogoUrl,
            DefaultDescription,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());
    }
}
