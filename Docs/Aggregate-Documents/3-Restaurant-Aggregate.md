# Aggregate Documentation: `Restaurant`

* **Version:** 1.1
* **Last Updated:** 2025-07-05
* **Source File:** `e:\source\repos\CA\YummyZoom\src\Domain\RestaurantAggregate\Restaurant.cs`

## 1. Overview

**Description:**
*Manages all information related to a restaurant, including its profile, location, contact details, and operational status. It acts as the consistency boundary for all restaurant-related operations.*

**Core Responsibilities:**

* Manages the lifecycle of a restaurant profile.
* Acts as the transactional boundary for all restaurant-related operations.
* Enforces business rules for restaurant information, such as name, description, and cuisine type.
* Manages the restaurant's location, geocoordinates, contact information, and business hours.
* Controls the restaurant's verification and order acceptance status.

## 2. Structure

* **Aggregate Root:** `Restaurant`
* **Key Child Entities:** None
* **Key Value Objects:**
  * `RestaurantId`: The unique identifier for the `Restaurant` aggregate.
  * `Address`: Represents the restaurant's physical location.
  * `GeoCoordinates`: Represents the restaurant's latitude and longitude.
  * `ContactInfo`: Represents the restaurant's contact details (phone and email).
  * `BusinessHours`: Represents the restaurant's operating hours.

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The primary way to create a `Restaurant` is through its static factory method.

```csharp
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
  string businessHours,
  double? latitude = null,
  double? longitude = null
)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `name` | `string` | The restaurant's name. |
| `logoUrl` | `string?` | The URL of the restaurant's logo. |
| `description` | `string` | A description of the restaurant. |
| `cuisineType` | `string` | The type of cuisine served. |
| `street` | `string` | The street address. |
| `city` | `string` | The city. |
| `state` | `string` | The state or province. |
| `zipCode` | `string` | The postal code. |
| `country` | `string` | The country. |
| `phoneNumber` | `string` | The contact phone number. |
| `email` | `string` | The contact email address. |
| `businessHours` | `string` | The business hours. |
| `latitude` | `double?` | Optional latitude (-90..90). |
| `longitude` | `double?` | Optional longitude (-180..180). |

**Validation Rules & Potential Errors:**

* `name` is required and has a maximum length. (Returns `RestaurantErrors.NameIsRequired`, `RestaurantErrors.NameTooLong`)
* `description` is required and has a maximum length. (Returns `RestaurantErrors.DescriptionIsRequired`, `RestaurantErrors.DescriptionTooLong`)
* `cuisineType` is required and has a maximum length. (Returns `RestaurantErrors.CuisineTypeIsRequired`, `RestaurantErrors.CuisineTypeTooLong`)
* `logoUrl` must be a valid URL if provided. (Returns `RestaurantErrors.InvalidLogoUrl`)
* All `Address` fields are required and have maximum lengths. (Returns various `Address` related errors)
* `phoneNumber` and `email` are required and must be in a valid format. (Returns various `ContactInfo` related errors)
* `businessHours` is required and has a maximum length. (Returns `RestaurantErrors.BusinessHoursFormatIsRequired`, `RestaurantErrors.BusinessHoursFormatTooLong`)
* If `latitude` and `longitude` are provided, they must be within valid ranges. (Returns `RestaurantErrors.LatitudeOutOfRange`, `RestaurantErrors.LongitudeOutOfRange`)

### 3.2. State Transitions & Commands (Public Methods)

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result ChangeName(string name)` | Updates the restaurant's name. | Name is required and within length limits. | `RestaurantErrors.NameIsRequired`, `RestaurantErrors.NameTooLong` |
| `Result UpdateDescription(string description)` | Updates the restaurant's description. | Description is required and within length limits. | `RestaurantErrors.DescriptionIsRequired`, `RestaurantErrors.DescriptionTooLong` |
| `Result ChangeCuisineType(string cuisineType)` | Updates the restaurant's cuisine type. | Cuisine type is required and within length limits. | `RestaurantErrors.CuisineTypeIsRequired`, `RestaurantErrors.CuisineTypeTooLong` |
| `Result UpdateLogo(string? logoUrl)` | Updates the restaurant's logo URL. | URL format is valid if provided. | `RestaurantErrors.InvalidLogoUrl` |
| `Result ChangeLocation(Address location)` | Updates the restaurant's location. | Location is not null. | `RestaurantErrors.LocationIsRequired` |
| `Result ChangeGeoCoordinates(double latitude, double longitude)` | Updates the restaurant's geo coordinates. | Latitude in [-90, 90], Longitude in [-180, 180]. | `RestaurantErrors.LatitudeOutOfRange`, `RestaurantErrors.LongitudeOutOfRange` |
| `Result UpdateContactInfo(ContactInfo contactInfo)` | Updates the restaurant's contact information. | Contact info is not null. | `RestaurantErrors.ContactInfoIsRequired` |
| `Result UpdateBusinessHours(BusinessHours businessHours)` | Updates the restaurant's business hours. | Business hours are not null. | `RestaurantErrors.BusinessHoursIsRequired` |
| `void Verify()` | Marks the restaurant as verified. | None. | None. |
| `void AcceptOrders()` | Sets the restaurant to accept orders. | None. | None. |
| `void DeclineOrders()` | Sets the restaurant to not accept orders. | None. | None. |
| `Result MarkAsDeleted(bool forceDelete = false)` | Marks the restaurant as deleted. | Cannot delete if accepting orders or verified unless force delete is true. | `RestaurantErrors.CannotDeleteWithActiveOrders`, `RestaurantErrors.CannotDeleteVerifiedRestaurantWithoutConfirmation` |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `RestaurantId` | The unique identifier of the aggregate. |
| `Name` | `string` | The restaurant's name. |
| `LogoUrl` | `string` | The URL of the restaurant's logo. |
| `Description` | `string` | A description of the restaurant. |
| `CuisineType` | `string` | The type of cuisine served. |
| `Location` | `Address` | The restaurant's physical location. |
| `GeoCoordinates` | `GeoCoordinates?` | The restaurant's optional geospatial coordinates. |
| `ContactInfo` | `ContactInfo` | The restaurant's contact details. |
| `BusinessHours` | `BusinessHours` | The restaurant's operating hours. |
| `IsVerified` | `bool` | Whether the restaurant is verified. |
| `IsAcceptingOrders` | `bool` | Whether the restaurant is currently accepting orders. |

## 5. Communication (Domain Events)

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `RestaurantCreated` | During the `Create` factory method. | Signals that a new restaurant has been successfully created. |
| `RestaurantNameChanged` | After a successful call to `ChangeName`. | Signals that the restaurant's name has changed. |
| `RestaurantDescriptionChanged` | After a successful call to `UpdateDescription`. | Signals that the restaurant's description has changed. |
| `RestaurantCuisineTypeChanged` | After a successful call to `ChangeCuisineType`. | Signals that the restaurant's cuisine type has changed. |
| `RestaurantLogoChanged` | After a successful call to `UpdateLogo`. | Signals that the restaurant's logo URL has changed. |
| `RestaurantLocationChanged` | After a successful call to `ChangeLocation`. | Signals that the restaurant's location has changed. |
| `RestaurantGeoCoordinatesChanged` | After a successful call to `ChangeGeoCoordinates`. | Signals that the restaurant's geospatial coordinates have changed. |
| `RestaurantContactInfoChanged` | After a successful call to `UpdateContactInfo`. | Signals that the restaurant's contact information has changed. |
| `RestaurantBusinessHoursChanged` | After a successful call to `UpdateBusinessHours`. | Signals that the restaurant's business hours have changed. |
| `RestaurantVerified` | After a successful call to `Verify`. | Signals that the restaurant has been verified. |
| `RestaurantAcceptingOrders` | After a successful call to `AcceptOrders`. | Signals that the restaurant is now accepting orders. |
| `RestaurantNotAcceptingOrders` | After a successful call to `DeclineOrders`. | Signals that the restaurant is no longer accepting orders. |
| `RestaurantBrandingUpdated` | After a successful call to `UpdateBranding`. | Signals that the restaurant's branding information has been updated. |
| `RestaurantProfileUpdated` | After a successful call to `UpdateCompleteProfile`. | Signals that the restaurant's entire profile has been updated. |
| `RestaurantUpdated` | After a successful call to `UpdateBasicInfo`. | Signals that the restaurant's basic information has been updated. |
| `RestaurantDeleted` | After a successful call to `MarkAsDeleted`. | Signals that the restaurant has been marked for deletion. |
