using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.RestaurantAggregate;

/// <summary>
/// Tests for Restaurant aggregate update functionality including granular update methods.
/// </summary>
[TestFixture]
public class RestaurantUpdateTests
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

    #region ChangeName Tests

    [Test]
    public void ChangeName_WithValidName_ShouldSucceedAndUpdateNameAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newName = "New Restaurant Name";

        // Act
        var result = restaurant.ChangeName(newName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Name.Should().Be(newName);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantNameChanged));
        
        var nameChangedEvent = restaurant.DomainEvents.OfType<RestaurantNameChanged>().Single();
        nameChangedEvent.OldName.Should().Be(DefaultName);
        nameChangedEvent.NewName.Should().Be(newName);
    }

    [Test]
    public void ChangeName_WithEmptyName_ShouldFailWithNameRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.ChangeName(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.NameIsRequired());
        restaurant.Name.Should().Be(DefaultName); // Should not change
    }

    [Test]
    public void ChangeName_WithTooLongName_ShouldFailWithNameTooLongError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var tooLongName = new string('a', 101); // Max is 100

        // Act
        var result = restaurant.ChangeName(tooLongName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.NameTooLong(100));
        restaurant.Name.Should().Be(DefaultName); // Should not change
    }

    #endregion

    #region UpdateDescription Tests

    [Test]
    public void UpdateDescription_WithValidDescription_ShouldSucceedAndUpdateDescriptionAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newDescription = "New restaurant description with more details about the food and atmosphere.";

        // Act
        var result = restaurant.UpdateDescription(newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Description.Should().Be(newDescription);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantDescriptionChanged));
        
        var descriptionChangedEvent = restaurant.DomainEvents.OfType<RestaurantDescriptionChanged>().Single();
        descriptionChangedEvent.OldDescription.Should().Be(DefaultDescription);
        descriptionChangedEvent.NewDescription.Should().Be(newDescription);
    }

    [Test]
    public void UpdateDescription_WithEmptyDescription_ShouldFailWithDescriptionRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateDescription(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.DescriptionIsRequired());
        restaurant.Description.Should().Be(DefaultDescription); // Should not change
    }

    [Test]
    public void UpdateDescription_WithTooLongDescription_ShouldFailWithDescriptionTooLongError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var tooLongDescription = new string('a', 501); // Max is 500

        // Act
        var result = restaurant.UpdateDescription(tooLongDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.DescriptionTooLong(500));
        restaurant.Description.Should().Be(DefaultDescription); // Should not change
    }

    #endregion

    #region ChangeCuisineType Tests

    [Test]
    public void ChangeCuisineType_WithValidCuisineType_ShouldSucceedAndUpdateCuisineTypeAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newCuisineType = "Italian";

        // Act
        var result = restaurant.ChangeCuisineType(newCuisineType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.CuisineType.Should().Be(newCuisineType);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantCuisineTypeChanged));
        
        var cuisineTypeChangedEvent = restaurant.DomainEvents.OfType<RestaurantCuisineTypeChanged>().Single();
        cuisineTypeChangedEvent.OldCuisineType.Should().Be(DefaultCuisineType);
        cuisineTypeChangedEvent.NewCuisineType.Should().Be(newCuisineType);
    }

    [Test]
    public void ChangeCuisineType_WithEmptyCuisineType_ShouldFailWithCuisineTypeRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.ChangeCuisineType(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.CuisineTypeIsRequired());
        restaurant.CuisineType.Should().Be(DefaultCuisineType); // Should not change
    }

    [Test]
    public void ChangeCuisineType_WithTooLongCuisineType_ShouldFailWithCuisineTypeTooLongError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var tooLongCuisineType = new string('a', 51); // Max is 50

        // Act
        var result = restaurant.ChangeCuisineType(tooLongCuisineType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.CuisineTypeTooLong(50));
        restaurant.CuisineType.Should().Be(DefaultCuisineType); // Should not change
    }

    #endregion

    #region UpdateLogo Tests

    [Test]
    public void UpdateLogo_WithValidLogoUrl_ShouldSucceedAndUpdateLogoAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newLogoUrl = "https://example.com/new-logo.png";

        // Act
        var result = restaurant.UpdateLogo(newLogoUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.LogoUrl.Should().Be(newLogoUrl);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantLogoChanged));
        
        var logoChangedEvent = restaurant.DomainEvents.OfType<RestaurantLogoChanged>().Single();
        logoChangedEvent.OldLogoUrl.Should().Be(DefaultLogoUrl);
        logoChangedEvent.NewLogoUrl.Should().Be(newLogoUrl);
    }

    [Test]
    public void UpdateLogo_WithNullLogoUrl_ShouldSucceedAndSetEmptyString()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateLogo(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.LogoUrl.Should().Be(string.Empty);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantLogoChanged));
    }

    [Test]
    public void UpdateLogo_WithInvalidLogoUrl_ShouldFailWithInvalidLogoUrlError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string invalidLogoUrl = "invalid-url";

        // Act
        var result = restaurant.UpdateLogo(invalidLogoUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.InvalidLogoUrl(invalidLogoUrl));
        restaurant.LogoUrl.Should().Be(DefaultLogoUrl); // Should not change
    }

    #endregion

    #region ChangeLocation Tests

    [Test]
    public void ChangeLocation_WithValidAddress_ShouldSucceedAndUpdateLocationAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var newAddress = Address.Create("456 Oak Ave", "New City", "New State", "67890", "New Country").Value;

        // Act
        var result = restaurant.ChangeLocation(newAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Location.Should().Be(newAddress);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantLocationChanged));
        
        var locationChangedEvent = restaurant.DomainEvents.OfType<RestaurantLocationChanged>().Single();
        locationChangedEvent.OldLocation.Should().Be(CreateValidAddress());
        locationChangedEvent.NewLocation.Should().Be(newAddress);
    }

    [Test]
    public void ChangeLocation_WithValidStrings_ShouldSucceedAndUpdateLocationAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newStreet = "456 Oak Ave";
        const string newCity = "New City";
        const string newState = "New State";
        const string newZipCode = "67890";
        const string newCountry = "New Country";

        // Act
        var result = restaurant.ChangeLocation(newStreet, newCity, newState, newZipCode, newCountry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Location.Street.Should().Be(newStreet);
        restaurant.Location.City.Should().Be(newCity);
        restaurant.Location.State.Should().Be(newState);
        restaurant.Location.ZipCode.Should().Be(newZipCode);
        restaurant.Location.Country.Should().Be(newCountry);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantLocationChanged));
    }

    [Test]
    public void ChangeLocation_WithNullAddress_ShouldFailWithLocationRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.ChangeLocation((Address)null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.LocationIsRequired());
    }

    [Test]
    public void ChangeLocation_WithInvalidStrings_ShouldFailWithAddressValidationError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.ChangeLocation(string.Empty, "City", "State", "12345", "Country");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.AddressStreetIsRequired());
    }

    #endregion

    #region UpdateContactInfo Tests

    [Test]
    public void UpdateContactInfo_WithValidContactInfo_ShouldSucceedAndUpdateContactInfoAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var newContactInfo = ContactInfo.Create("987-654-3210", "new@example.com").Value;

        // Act
        var result = restaurant.UpdateContactInfo(newContactInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.ContactInfo.Should().Be(newContactInfo);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantContactInfoChanged));
        
        var contactInfoChangedEvent = restaurant.DomainEvents.OfType<RestaurantContactInfoChanged>().Single();
        contactInfoChangedEvent.OldContactInfo.Should().Be(CreateValidContactInfo());
        contactInfoChangedEvent.NewContactInfo.Should().Be(newContactInfo);
    }

    [Test]
    public void UpdateContactInfo_WithValidStrings_ShouldSucceedAndUpdateContactInfoAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newPhoneNumber = "987-654-3210";
        const string newEmail = "new@example.com";

        // Act
        var result = restaurant.UpdateContactInfo(newPhoneNumber, newEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.ContactInfo.PhoneNumber.Should().Be(newPhoneNumber);
        restaurant.ContactInfo.Email.Should().Be(newEmail);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantContactInfoChanged));
    }

    [Test]
    public void UpdateContactInfo_WithNullContactInfo_ShouldFailWithContactInfoRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateContactInfo((ContactInfo)null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactInfoIsRequired());
    }

    [Test]
    public void UpdateContactInfo_WithInvalidEmail_ShouldFailWithEmailValidationError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateContactInfo("123-456-7890", "invalid-email");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailInvalidFormat("invalid-email"));
    }

    #endregion

    #region UpdateBusinessHours Tests

    [Test]
    public void UpdateBusinessHours_WithValidBusinessHours_ShouldSucceedAndUpdateBusinessHoursAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        var newBusinessHours = BusinessHours.Create("Sat-Sun: 10am-4pm").Value;

        // Act
        var result = restaurant.UpdateBusinessHours(newBusinessHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.BusinessHours.Should().Be(newBusinessHours);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantBusinessHoursChanged));
        
        var businessHoursChangedEvent = restaurant.DomainEvents.OfType<RestaurantBusinessHoursChanged>().Single();
        businessHoursChangedEvent.OldBusinessHours.Should().Be(CreateValidBusinessHours());
        businessHoursChangedEvent.NewBusinessHours.Should().Be(newBusinessHours);
    }

    [Test]
    public void UpdateBusinessHours_WithValidString_ShouldSucceedAndUpdateBusinessHoursAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newHours = "Sat-Sun: 10am-4pm";

        // Act
        var result = restaurant.UpdateBusinessHours(newHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.BusinessHours.Hours.Should().Be(newHours);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantBusinessHoursChanged));
    }

    [Test]
    public void UpdateBusinessHours_WithNullBusinessHours_ShouldFailWithBusinessHoursRequiredError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateBusinessHours((BusinessHours)null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursIsRequired());
    }

    [Test]
    public void UpdateBusinessHours_WithEmptyString_ShouldFailWithBusinessHoursValidationError()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();

        // Act
        var result = restaurant.UpdateBusinessHours(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.BusinessHoursFormatIsRequired());
    }

    #endregion

    #region Composite Update Tests

    [Test]
    public void UpdateBranding_WithValidInputs_ShouldSucceedAndUpdateAllBrandingFieldsAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newName = "New Restaurant Name";
        const string newLogoUrl = "https://example.com/new-logo.png";
        const string newDescription = "New restaurant description with updated branding.";

        // Act
        var result = restaurant.UpdateBranding(newName, newLogoUrl, newDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Name.Should().Be(newName);
        restaurant.LogoUrl.Should().Be(newLogoUrl);
        restaurant.Description.Should().Be(newDescription);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantBrandingUpdated));
        
        var brandingUpdatedEvent = restaurant.DomainEvents.OfType<RestaurantBrandingUpdated>().Single();
        brandingUpdatedEvent.OldName.Should().Be(DefaultName);
        brandingUpdatedEvent.NewName.Should().Be(newName);
        brandingUpdatedEvent.OldLogoUrl.Should().Be(DefaultLogoUrl);
        brandingUpdatedEvent.NewLogoUrl.Should().Be(newLogoUrl);
        brandingUpdatedEvent.OldDescription.Should().Be(DefaultDescription);
        brandingUpdatedEvent.NewDescription.Should().Be(newDescription);
    }

    [Test]
    public void UpdateBranding_WithInvalidName_ShouldFailAndNotUpdateAnyFields()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newLogoUrl = "https://example.com/new-logo.png";
        const string newDescription = "New restaurant description with updated branding.";

        // Act
        var result = restaurant.UpdateBranding(string.Empty, newLogoUrl, newDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.NameIsRequired());
        restaurant.Name.Should().Be(DefaultName); // Should not change
        restaurant.LogoUrl.Should().Be(DefaultLogoUrl); // Should not change
        restaurant.Description.Should().Be(DefaultDescription); // Should not change
    }

    [Test]
    public void UpdateBasicInfo_WithValidInputs_ShouldSucceedAndUpdateBasicFieldsAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newName = "New Restaurant Name";
        const string newDescription = "New restaurant description.";
        const string newCuisineType = "Italian";

        // Act
        var result = restaurant.UpdateBasicInfo(newName, newDescription, newCuisineType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Name.Should().Be(newName);
        restaurant.Description.Should().Be(newDescription);
        restaurant.CuisineType.Should().Be(newCuisineType);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantUpdated));
    }

    [Test]
    public void UpdateCompleteProfile_WithValidInputs_ShouldSucceedAndUpdateAllFieldsAndRaiseEvent()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newName = "New Restaurant Name";
        const string newDescription = "New restaurant description.";
        const string newCuisineType = "Italian";
        const string newLogoUrl = "https://example.com/new-logo.png";
        const string newStreet = "456 Oak Ave";
        const string newCity = "New City";
        const string newState = "New State";
        const string newZipCode = "67890";
        const string newCountry = "New Country";
        const string newPhoneNumber = "987-654-3210";
        const string newEmail = "new@example.com";
        const string newBusinessHours = "Sat-Sun: 10am-4pm";

        // Act
        var result = restaurant.UpdateCompleteProfile(
            newName,
            newDescription,
            newCuisineType,
            newLogoUrl,
            newStreet,
            newCity,
            newState,
            newZipCode,
            newCountry,
            newPhoneNumber,
            newEmail,
            newBusinessHours);

        // Assert
        result.IsSuccess.Should().BeTrue();
        restaurant.Name.Should().Be(newName);
        restaurant.Description.Should().Be(newDescription);
        restaurant.CuisineType.Should().Be(newCuisineType);
        restaurant.LogoUrl.Should().Be(newLogoUrl);
        restaurant.Location.Street.Should().Be(newStreet);
        restaurant.Location.City.Should().Be(newCity);
        restaurant.ContactInfo.PhoneNumber.Should().Be(newPhoneNumber);
        restaurant.ContactInfo.Email.Should().Be(newEmail);
        restaurant.BusinessHours.Hours.Should().Be(newBusinessHours);
        restaurant.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(RestaurantProfileUpdated));
        
        var profileUpdatedEvent = restaurant.DomainEvents.OfType<RestaurantProfileUpdated>().Single();
        profileUpdatedEvent.OldName.Should().Be(DefaultName);
        profileUpdatedEvent.NewName.Should().Be(newName);
    }

    [Test]
    public void UpdateCompleteProfile_WithInvalidEmail_ShouldFailAndNotUpdateAnyFields()
    {
        // Arrange
        var restaurant = CreateTestRestaurant();
        const string newName = "New Restaurant Name";
        const string newDescription = "New restaurant description.";
        const string newCuisineType = "Italian";
        const string newLogoUrl = "https://example.com/new-logo.png";
        const string newStreet = "456 Oak Ave";
        const string newCity = "New City";
        const string newState = "New State";
        const string newZipCode = "67890";
        const string newCountry = "New Country";
        const string newPhoneNumber = "987-654-3210";
        const string invalidEmail = "invalid-email";
        const string newBusinessHours = "Sat-Sun: 10am-4pm";

        // Act
        var result = restaurant.UpdateCompleteProfile(
            newName,
            newDescription,
            newCuisineType,
            newLogoUrl,
            newStreet,
            newCity,
            newState,
            newZipCode,
            newCountry,
            newPhoneNumber,
            invalidEmail,
            newBusinessHours);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RestaurantErrors.ContactEmailInvalidFormat(invalidEmail));
        restaurant.Name.Should().Be(DefaultName); // Should not change
        restaurant.Description.Should().Be(DefaultDescription); // Should not change
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
