namespace YummyZoom.Application.RestaurantRegistrations.Queries.Common;

public sealed record RegistrationSummaryDto(
    Guid RegistrationId,
    string Name,
    string City,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewNote,
    Guid SubmitterUserId);

public sealed record RegistrationDetailForAdminDto(
    Guid RegistrationId,
    string Name,
    string Description,
    string CuisineType,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string PhoneNumber,
    string Email,
    string BusinessHours,
    string? LogoUrl,
    double? Latitude,
    double? Longitude,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewNote,
    Guid SubmitterUserId,
    string SubmitterName,
    Guid? ReviewedByUserId);
