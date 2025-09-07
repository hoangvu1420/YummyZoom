using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when an item is removed from a team cart.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="TeamCartItemId">The ID of the removed team cart item.</param>
/// <param name="RemovedByUserId">The ID of the user who removed the item.</param>
public sealed record ItemRemovedFromTeamCart(
    TeamCartId TeamCartId,
    TeamCartItemId TeamCartItemId,
    UserId RemovedByUserId) : DomainEventBase;

