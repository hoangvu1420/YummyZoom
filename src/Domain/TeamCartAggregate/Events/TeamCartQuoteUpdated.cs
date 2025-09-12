using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Raised whenever the TeamCart aggregate computes a new Quote Lite snapshot.
/// Carries the version and per-member quoted amounts for projection into the VM.
/// </summary>
public sealed record TeamCartQuoteUpdated(
    TeamCartId TeamCartId,
    long QuoteVersion,
    IReadOnlyDictionary<Guid, decimal> MemberQuotedAmounts,
    string Currency
) : DomainEventBase;

