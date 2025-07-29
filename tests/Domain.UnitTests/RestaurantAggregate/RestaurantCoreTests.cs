using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate;

/// <summary>
/// Tests for core Restaurant aggregate functionality including creation and lifecycle management.
/// </summary>
[TestFixture]
public class RestaurantCoreTests
{
    private const string DefaultName = "Test Restaurant";
    private const string DefaultLogoUrl = "http://example.com/logo.png";
    private const string DefaultDescription = "Test Description";
    private const string DefaultCuisineType = "Test Cuisine";
    private const string DefaultStreet = "123 Main St";
    private const string DefaultCity = "Test City";
    private const string DefaultState = "Test State";
    private const string DefaultZipCode = "12345";
    private const string DefaultCountry = "Test Country";
    private const string DefaultPhoneNumber = "123-456-7890";
    private const string DefaultEmail = "test@example.com";
    private const string DefaultBusinessHours = "Mon-Fri: 9am-5pm";

    private static Address CreateValidAddress() => Address.Create(DefaultStreet, DefaultCity, DefaultState, DefaultZipCode, DefaultCountry).Value;
    private static ContactInfo CreateValidContactInfo() => ContactInfo.Create(DefaultPhoneNumber, DefaultEmail).Value;
    private static BusinessHours CreateValidBusinessHours() => BusinessHours.Create(DefaultBusinessHours).Value;

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeRestaurantCorrectly()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName, 
            DefaultLogoUrl, 
            DefaultDescription, 
            DefaultCuisineType, 
            CreateValidAddress(), 
            CreateValidContactInfo(), 
            CreateValidBusinessHours());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var restaurant = result.Value;
        
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
    public void Create_WithValidInputsUsingStrings_ShouldSucceedAndInitializeRestaurantCorrectly()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName,
            DefaultLogoUrl,
            DefaultDescription,
            DefaultCuisineType,
            DefaultStreet,
            DefaultCity,
            DefaultState,
            DefaultZipCode,
            DefaultCountry,
            DefaultPhoneNumber,
            DefaultEmail,
            DefaultBusinessHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var restaurant = result.Value;
        
        restaurant.Should().NotBeNull();
        restaurant.Id.Value.Should().NotBe(Guid.Empty);
        restaurant.Name.Should().Be(DefaultName);
        restaurant.LogoUrl.Should().Be(DefaultLogoUrl);
        restaurant.Description.Should().Be(DefaultDescription);
        restaurant.CuisineType.Should().Be(DefaultCuisineType);
        restaurant.IsVerified.Should().BeFalse();
        restaurant.IsAcceptingOrders.Should().BeFalse();
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantCreated));
    }

    [Test]
    public void Create_WithNullOrEmptyName_ShouldFailWithNameRequiredError()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            string.Empty,
            DefaultLogoUrl,
            DefaultDescription,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.NameIsRequired());
    }

    [Test]
    public void Create_WithNullOrEmptyDescription_ShouldFailWithDescriptionRequiredError()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName,
            DefaultLogoUrl,
            string.Empty,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.DescriptionIsRequired());
    }

    [Test]
    public void Create_WithNullOrEmptyCuisineType_ShouldFailWithCuisineTypeRequiredError()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName,
            DefaultLogoUrl,
            DefaultDescription,
            string.Empty,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.CuisineTypeIsRequired());
    }

    [Test]
    public void Create_WithInvalidLogoUrl_ShouldFailWithInvalidLogoUrlError()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName,
            "invalid-url",
            DefaultDescription,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.InvalidLogoUrl("invalid-url"));
    }

    [Test]
    public void Create_WithNullLogoUrl_ShouldSucceed()
    {
        // Arrange & Act
        var result = Restaurant.Create(
            DefaultName,
            null,
            DefaultDescription,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be(string.Empty);
    }

    #endregion

    #region Verification Tests

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

    #endregion

    #region Order Management Tests

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

    #endregion

    #region Helper Methods

    private Restaurant CreateTestRestaurant()
    {
        var result = Restaurant.Create(
            DefaultName,
            DefaultLogoUrl,
            DefaultDescription,
            DefaultCuisineType,
            CreateValidAddress(),
            CreateValidContactInfo(),
            CreateValidBusinessHours());
        
        return result.Value;
    }

    #endregion
}
