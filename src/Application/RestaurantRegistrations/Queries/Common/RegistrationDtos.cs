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

