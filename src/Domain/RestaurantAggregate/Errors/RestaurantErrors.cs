
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.Errors;

public static class RestaurantErrors
{
    public static Error InvalidRestaurantId(string value) => Error.Validation(
        "Restaurant.InvalidRestaurantId",
        $"Restaurant ID '{value}' is not a valid GUID.");

    // Restaurant-level validation errors
    public static Error NameIsRequired() => Error.Validation(
        "Restaurant.NameIsRequired",
        "Restaurant name is required.");

    public static Error NameTooLong(int maxLength) => Error.Validation(
        "Restaurant.NameTooLong",
        $"Restaurant name cannot exceed {maxLength} characters.");

    public static Error DescriptionIsRequired() => Error.Validation(
        "Restaurant.DescriptionIsRequired",
        "Restaurant description is required.");

    public static Error DescriptionTooLong(int maxLength) => Error.Validation(
        "Restaurant.DescriptionTooLong",
        $"Restaurant description cannot exceed {maxLength} characters.");

    public static Error CuisineTypeIsRequired() => Error.Validation(
        "Restaurant.CuisineTypeIsRequired",
        "Cuisine type is required.");

    public static Error CuisineTypeTooLong(int maxLength) => Error.Validation(
        "Restaurant.CuisineTypeTooLong",
        $"Cuisine type cannot exceed {maxLength} characters.");

    public static Error InvalidLogoUrl(string value) => Error.Validation(
        "Restaurant.InvalidLogoUrl",
        $"Logo URL '{value}' format is invalid.");

    public static Error LocationIsRequired() => Error.Validation(
        "Restaurant.LocationIsRequired",
        "Restaurant location is required.");

    public static Error ContactInfoIsRequired() => Error.Validation(
        "Restaurant.ContactInfoIsRequired",
        "Restaurant contact information is required.");

    public static Error BusinessHoursIsRequired() => Error.Validation(
        "Restaurant.BusinessHoursIsRequired",
        "Restaurant business hours are required.");

    // Address validation errors
    public static Error AddressStreetIsRequired() => Error.Validation(
        "Restaurant.Address.StreetIsRequired",
        "Street address is required.");

    public static Error AddressCityIsRequired() => Error.Validation(
        "Restaurant.Address.CityIsRequired",
        "City is required.");

    public static Error AddressStateIsRequired() => Error.Validation(
        "Restaurant.Address.StateIsRequired",
        "State is required.");

    public static Error AddressZipCodeIsRequired() => Error.Validation(
        "Restaurant.Address.ZipCodeIsRequired",
        "ZIP code is required.");

    public static Error AddressCountryIsRequired() => Error.Validation(
        "Restaurant.Address.CountryIsRequired",
        "Country is required.");

    public static Error AddressFieldTooLong(string field, int maxLength) => Error.Validation(
        "Restaurant.Address.FieldTooLong",
        $"Address field '{field}' exceeds maximum length of {maxLength}.");

    // ContactInfo validation errors
    public static Error ContactEmailIsRequired() => Error.Validation(
        "Restaurant.Contact.EmailIsRequired",
        "Email address is required.");

    public static Error ContactEmailInvalidFormat(string value) => Error.Validation(
        "Restaurant.Contact.EmailInvalidFormat",
        $"Email address '{value}' format is invalid.");

    public static Error ContactPhoneIsRequired() => Error.Validation(
        "Restaurant.Contact.PhoneIsRequired",
        "Phone number is required.");

    public static Error ContactPhoneInvalidFormat(string value) => Error.Validation(
        "Restaurant.Contact.PhoneInvalidFormat",
        $"Phone number '{value}' format is invalid.");

    // BusinessHours validation errors
    public static Error BusinessHoursFormatIsRequired() => Error.Validation(
        "Restaurant.BusinessHours.FormatIsRequired",
        "Business hours format is required.");

    public static Error BusinessHoursFormatTooLong(int maxLength) => Error.Validation(
        "Restaurant.BusinessHours.FormatTooLong",
        $"Business hours format cannot exceed {maxLength} characters.");

    // Deletion validation errors
    public static Error CannotDeleteWithActiveOrders() => Error.Validation(
        "Restaurant.CannotDeleteWithActiveOrders",
        "Cannot delete restaurant that has active orders. Please complete or cancel all active orders first.");

    public static Error CannotDeleteVerifiedRestaurantWithoutConfirmation() => Error.Validation(
        "Restaurant.CannotDeleteVerifiedRestaurantWithoutConfirmation",
        "Cannot delete verified restaurant without explicit confirmation due to potential impact on existing customers.");
}
