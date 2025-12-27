namespace YummyZoom.Application.Payouts.Queries.Common;

public record PayoutEligibilityDto(
    bool IsEligible,
    decimal AvailableAmount,
    string Currency,
    bool HasPayoutMethod,
    DateTimeOffset? NextEligibleAt,
    string? IneligibilityReason);

public record PayoutSummaryDto(
    Guid PayoutId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? FailedAt);

public record PayoutDetailsDto(
    Guid PayoutId,
    Guid RestaurantId,
    Guid RestaurantAccountId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? FailedAt,
    string? ProviderReferenceId,
    string? FailureReason,
    string IdempotencyKey);
