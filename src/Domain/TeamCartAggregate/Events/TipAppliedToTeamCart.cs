using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a tip is applied/updated on a TeamCart.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="TipAmount">The tip amount (with currency).</param>
public sealed record TipAppliedToTeamCart(
    TeamCartId TeamCartId,
    Money TipAmount) : DomainEventBase;

