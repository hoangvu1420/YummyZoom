using YummyZoom.Domain.RestaurantAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;
using System.Text.RegularExpressions;

namespace YummyZoom.Domain.RestaurantAggregate;

public sealed class Restaurant : AggregateRoot<RestaurantId, Guid>, IAuditableEntity, ISoftDeletableEntity
{
    #region Properties

    public string Name { get; private set; }
    public string LogoUrl { get; private set; }
    public string Description { get; private set; }
    public string CuisineType { get; private set; }
    public Address Location { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public BusinessHours BusinessHours { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsAcceptingOrders { get; private set; }

    // Audit properties
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    #region Constructors

    private Restaurant(
        RestaurantId id,
        string name,
        string logoUrl,
        string description,
        string cuisineType,
        Address location,
        ContactInfo contactInfo,
        BusinessHours businessHours,
        bool isVerified,
        bool isAcceptingOrders)
        : base(id)
    {
        Name = name;
        LogoUrl = logoUrl;
        Description = description;
        CuisineType = cuisineType;
        Location = location;
        ContactInfo = contactInfo;
        BusinessHours = businessHours;
        IsVerified = isVerified;
        IsAcceptingOrders = isAcceptingOrders;
    }

    #endregion

    #region Static Factory Methods

    public static Result<Restaurant> Create(
        string name,
        string? logoUrl,
        string description,
        string cuisineType,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string phoneNumber,
        string email,
        string businessHours)
    {
        // Validate restaurant-level fields
        var validationResult = ValidateRestaurantFields(name, logoUrl, description, cuisineType);
        if (validationResult.IsFailure)
            return Result.Failure<Restaurant>(validationResult.Error);

        // Create value objects with validation
        var addressResult = Address.Create(street, city, state, zipCode, country);
        if (addressResult.IsFailure)
            return Result.Failure<Restaurant>(addressResult.Error);

        var contactInfoResult = ContactInfo.Create(phoneNumber, email);
        if (contactInfoResult.IsFailure)
            return Result.Failure<Restaurant>(contactInfoResult.Error);

        var businessHoursResult = BusinessHours.Create(businessHours);
        if (businessHoursResult.IsFailure)
            return Result.Failure<Restaurant>(businessHoursResult.Error);

        // Create the restaurant
        var restaurant = new Restaurant(
            RestaurantId.CreateUnique(),
            name.Trim(),
            logoUrl?.Trim() ?? string.Empty,
            description.Trim(),
            cuisineType.Trim(),
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            isVerified: false,
            isAcceptingOrders: false);

        restaurant.AddDomainEvent(new RestaurantCreated((RestaurantId)restaurant.Id));

        return Result.Success(restaurant);
    }

    public static Result<Restaurant> Create(
        string name,
        string? logoUrl,
        string description,
        string cuisineType,
        Address location,
        ContactInfo contactInfo,
        BusinessHours businessHours)
    {
        // Validate restaurant-level fields
        var validationResult = ValidateRestaurantFields(name, logoUrl, description, cuisineType);
        if (validationResult.IsFailure)
            return Result.Failure<Restaurant>(validationResult.Error);

        // Validate required objects
        if (location is null)
            return Result.Failure<Restaurant>(RestaurantErrors.LocationIsRequired());
        
        if (contactInfo is null)
            return Result.Failure<Restaurant>(RestaurantErrors.ContactInfoIsRequired());
        
        if (businessHours is null)
            return Result.Failure<Restaurant>(RestaurantErrors.BusinessHoursIsRequired());

        // Create the restaurant
        var restaurant = new Restaurant(
            RestaurantId.CreateUnique(),
            name.Trim(),
            logoUrl?.Trim() ?? string.Empty,
            description.Trim(),
            cuisineType.Trim(),
            location,
            contactInfo,
            businessHours,
            isVerified: false,
            isAcceptingOrders: false);

        restaurant.AddDomainEvent(new RestaurantCreated((RestaurantId)restaurant.Id));

        return Result.Success(restaurant);
    }

    #endregion

    #region Public Methods - Lifecycle

    public void Verify()
    {
        if (IsVerified) return;
        IsVerified = true;
        AddDomainEvent(new RestaurantVerified((RestaurantId)Id));
    }

    public void AcceptOrders()
    {
        if (IsAcceptingOrders) return;
        IsAcceptingOrders = true;
        AddDomainEvent(new RestaurantAcceptingOrders((RestaurantId)Id));
    }

    public void DeclineOrders()
    {
        if (!IsAcceptingOrders) return;
        IsAcceptingOrders = false;
        AddDomainEvent(new RestaurantNotAcceptingOrders((RestaurantId)Id));
    }

    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return Result.Success();
        }

        // Business rule: Cannot delete restaurant with active orders
        if (IsAcceptingOrders)
        {
            return Result.Failure(RestaurantErrors.CannotDeleteWithActiveOrders());
        }

        // Business rule: Verified restaurants require explicit confirmation
        if (IsVerified)
        {
            return Result.Failure(RestaurantErrors.CannotDeleteVerifiedRestaurantWithoutConfirmation());
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new RestaurantDeleted((RestaurantId)Id));

        return Result.Success();
    }

    #endregion

    #region Public Methods - Granular Updates

    public Result ChangeName(string name)
    {
        const int maxNameLength = 100;

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(RestaurantErrors.NameIsRequired());
        
        if (name.Length > maxNameLength)
            return Result.Failure(RestaurantErrors.NameTooLong(maxNameLength));

        var oldName = Name;
        Name = name.Trim();
        
        AddDomainEvent(new RestaurantNameChanged((RestaurantId)Id, oldName, Name));
        
        return Result.Success();
    }

    public Result UpdateDescription(string description)
    {
        const int maxDescriptionLength = 500;

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(RestaurantErrors.DescriptionIsRequired());
        
        if (description.Length > maxDescriptionLength)
            return Result.Failure(RestaurantErrors.DescriptionTooLong(maxDescriptionLength));

        var oldDescription = Description;
        Description = description.Trim();
        
        AddDomainEvent(new RestaurantDescriptionChanged((RestaurantId)Id, oldDescription, Description));
        
        return Result.Success();
    }

    public Result ChangeCuisineType(string cuisineType)
    {
        const int maxCuisineTypeLength = 50;

        if (string.IsNullOrWhiteSpace(cuisineType))
            return Result.Failure(RestaurantErrors.CuisineTypeIsRequired());
        
        if (cuisineType.Length > maxCuisineTypeLength)
            return Result.Failure(RestaurantErrors.CuisineTypeTooLong(maxCuisineTypeLength));

        var oldCuisineType = CuisineType;
        CuisineType = cuisineType.Trim();
        
        AddDomainEvent(new RestaurantCuisineTypeChanged((RestaurantId)Id, oldCuisineType, CuisineType));
        
        return Result.Success();
    }

    public Result UpdateLogo(string? logoUrl)
    {
        // Logo URL is optional, but if provided must be valid
        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var urlPattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$";
            if (!Regex.IsMatch(logoUrl, urlPattern))
                return Result.Failure(RestaurantErrors.InvalidLogoUrl(logoUrl));
        }

        var oldLogoUrl = LogoUrl;
        LogoUrl = logoUrl?.Trim() ?? string.Empty;
        
        AddDomainEvent(new RestaurantLogoChanged((RestaurantId)Id, oldLogoUrl, LogoUrl));
        
        return Result.Success();
    }

    public Result ChangeLocation(Address location)
    {
        if (location is null)
            return Result.Failure(RestaurantErrors.LocationIsRequired());

        var oldLocation = Location;
        Location = location;
        
        AddDomainEvent(new RestaurantLocationChanged((RestaurantId)Id, oldLocation, Location));
        
        return Result.Success();
    }

    public Result ChangeLocation(string street, string city, string state, string zipCode, string country)
    {
        var addressResult = Address.Create(street, city, state, zipCode, country);
        if (addressResult.IsFailure)
            return Result.Failure(addressResult.Error);

        var oldLocation = Location;
        Location = addressResult.Value;
        
        AddDomainEvent(new RestaurantLocationChanged((RestaurantId)Id, oldLocation, Location));
        
        return Result.Success();
    }

    public Result UpdateContactInfo(ContactInfo contactInfo)
    {
        if (contactInfo is null)
            return Result.Failure(RestaurantErrors.ContactInfoIsRequired());

        var oldContactInfo = ContactInfo;
        ContactInfo = contactInfo;
        
        AddDomainEvent(new RestaurantContactInfoChanged((RestaurantId)Id, oldContactInfo, ContactInfo));
        
        return Result.Success();
    }

    public Result UpdateContactInfo(string phoneNumber, string email)
    {
        var contactInfoResult = ContactInfo.Create(phoneNumber, email);
        if (contactInfoResult.IsFailure)
            return Result.Failure(contactInfoResult.Error);

        var oldContactInfo = ContactInfo;
        ContactInfo = contactInfoResult.Value;
        
        AddDomainEvent(new RestaurantContactInfoChanged((RestaurantId)Id, oldContactInfo, ContactInfo));
        
        return Result.Success();
    }

    public Result UpdateBusinessHours(BusinessHours businessHours)
    {
        if (businessHours is null)
            return Result.Failure(RestaurantErrors.BusinessHoursIsRequired());

        var oldBusinessHours = BusinessHours;
        BusinessHours = businessHours;
        
        AddDomainEvent(new RestaurantBusinessHoursChanged((RestaurantId)Id, oldBusinessHours, BusinessHours));
        
        return Result.Success();
    }

    public Result UpdateBusinessHours(string hours)
    {
        var businessHoursResult = BusinessHours.Create(hours);
        if (businessHoursResult.IsFailure)
            return Result.Failure(businessHoursResult.Error);

        var oldBusinessHours = BusinessHours;
        BusinessHours = businessHoursResult.Value;
        
        AddDomainEvent(new RestaurantBusinessHoursChanged((RestaurantId)Id, oldBusinessHours, BusinessHours));
        
        return Result.Success();
    }

    #endregion

    #region Public Methods - Composite Updates

    public Result UpdateBranding(string name, string? logoUrl, string description)
    {
        // Validate all branding fields first
        var nameValidation = ValidateField(name, 100, RestaurantErrors.NameIsRequired, RestaurantErrors.NameTooLong);
        if (nameValidation.IsFailure)
            return nameValidation;

        var descriptionValidation = ValidateField(description, 500, RestaurantErrors.DescriptionIsRequired, RestaurantErrors.DescriptionTooLong);
        if (descriptionValidation.IsFailure)
            return descriptionValidation;

        // Validate logo URL if provided
        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var urlPattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$";
            if (!Regex.IsMatch(logoUrl, urlPattern))
                return Result.Failure(RestaurantErrors.InvalidLogoUrl(logoUrl));
        }

        // Capture old values for the event
        var oldName = Name;
        var oldLogoUrl = LogoUrl;
        var oldDescription = Description;

        // Update all branding properties atomically
        Name = name.Trim();
        Description = description.Trim();
        LogoUrl = logoUrl?.Trim() ?? string.Empty;
        
        AddDomainEvent(new RestaurantBrandingUpdated(
            (RestaurantId)Id, 
            oldName, 
            Name, 
            oldLogoUrl, 
            LogoUrl, 
            oldDescription, 
            Description));
        
        return Result.Success();
    }

    public Result UpdateBasicInfo(string name, string description, string cuisineType)
    {
        // Validate all fields first
        var nameValidation = ValidateField(name, 100, RestaurantErrors.NameIsRequired, RestaurantErrors.NameTooLong);
        if (nameValidation.IsFailure)
            return nameValidation;

        var descriptionValidation = ValidateField(description, 500, RestaurantErrors.DescriptionIsRequired, RestaurantErrors.DescriptionTooLong);
        if (descriptionValidation.IsFailure)
            return descriptionValidation;

        var cuisineValidation = ValidateField(cuisineType, 50, RestaurantErrors.CuisineTypeIsRequired, RestaurantErrors.CuisineTypeTooLong);
        if (cuisineValidation.IsFailure)
            return cuisineValidation;

        // Update all properties atomically
        Name = name.Trim();
        Description = description.Trim();
        CuisineType = cuisineType.Trim();
        
        AddDomainEvent(new RestaurantUpdated((RestaurantId)Id));
        
        return Result.Success();
    }

    public Result UpdateCompleteProfile(
        string name, 
        string description, 
        string cuisineType, 
        string? logoUrl,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string phoneNumber,
        string email,
        string businessHours)
    {
        // Validate restaurant-level fields
        var validationResult = ValidateRestaurantFields(name, logoUrl, description, cuisineType);
        if (validationResult.IsFailure)
            return validationResult;

        // Create value objects with validation
        var addressResult = Address.Create(street, city, state, zipCode, country);
        if (addressResult.IsFailure)
            return Result.Failure(addressResult.Error);

        var contactInfoResult = ContactInfo.Create(phoneNumber, email);
        if (contactInfoResult.IsFailure)
            return Result.Failure(contactInfoResult.Error);

        var businessHoursResult = BusinessHours.Create(businessHours);
        if (businessHoursResult.IsFailure)
            return Result.Failure(businessHoursResult.Error);

        // Capture old values for the event
        var oldName = Name;
        var oldDescription = Description;
        var oldCuisineType = CuisineType;
        var oldLogoUrl = LogoUrl;
        var oldLocation = Location;
        var oldContactInfo = ContactInfo;
        var oldBusinessHours = BusinessHours;

        // Update all properties atomically
        Name = name.Trim();
        Description = description.Trim();
        CuisineType = cuisineType.Trim();
        LogoUrl = logoUrl?.Trim() ?? string.Empty;
        Location = addressResult.Value;
        ContactInfo = contactInfoResult.Value;
        BusinessHours = businessHoursResult.Value;
        
        AddDomainEvent(new RestaurantProfileUpdated(
            (RestaurantId)Id,
            oldName,
            Name,
            oldDescription,
            Description,
            oldCuisineType,
            CuisineType,
            oldLogoUrl,
            LogoUrl,
            oldLocation,
            Location,
            oldContactInfo,
            ContactInfo,
            oldBusinessHours,
            BusinessHours));
        
        return Result.Success();
    }

    #endregion

    #region Private Helper Methods

    private static Result ValidateRestaurantFields(string name, string? logoUrl, string description, string cuisineType)
    {
        const int maxNameLength = 100;
        const int maxDescriptionLength = 500;
        const int maxCuisineTypeLength = 50;

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(RestaurantErrors.NameIsRequired());
        
        if (name.Length > maxNameLength)
            return Result.Failure(RestaurantErrors.NameTooLong(maxNameLength));

        // Validate description
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(RestaurantErrors.DescriptionIsRequired());
        
        if (description.Length > maxDescriptionLength)
            return Result.Failure(RestaurantErrors.DescriptionTooLong(maxDescriptionLength));

        // Validate cuisine type
        if (string.IsNullOrWhiteSpace(cuisineType))
            return Result.Failure(RestaurantErrors.CuisineTypeIsRequired());
        
        if (cuisineType.Length > maxCuisineTypeLength)
            return Result.Failure(RestaurantErrors.CuisineTypeTooLong(maxCuisineTypeLength));

        // Validate logo URL (optional, but if provided must be valid)
        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var urlPattern = @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$";
            if (!Regex.IsMatch(logoUrl, urlPattern))
                return Result.Failure(RestaurantErrors.InvalidLogoUrl(logoUrl));
        }

        return Result.Success();
    }

    private static Result ValidateField(string value, int maxLength, Func<Error> requiredError, Func<int, Error> tooLongError)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure(requiredError());
        
        if (value.Length > maxLength)
            return Result.Failure(tooLongError(maxLength));

        return Result.Success();
    }

    #endregion

#pragma warning disable CS8618
    private Restaurant() { }
#pragma warning restore CS8618
}
