using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a team cart item's quantity is updated.
/// </summary>
public sealed record ItemQuantityUpdatedInTeamCart(
    TeamCartId TeamCartId,
    TeamCartItemId TeamCartItemId,
    UserId UpdatedByUserId,
    int OldQuantity,
    int NewQuantity) : DomainEventBase;

