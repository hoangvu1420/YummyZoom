namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Lightweight row used for EF JSONB persistence of per-member quoted totals.
/// Lives in Domain to avoid cross-assembly type references.
/// </summary>
public sealed record MemberTotalRow(Guid UserId, decimal Amount, string Currency);

