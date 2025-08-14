using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when an item is added to a team cart.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart to which the item was added.</param>
/// <param name="TeamCartItemId">The ID of the newly added team cart item.</param>
/// <param name="AddedByUserId">The ID of the user who added the item.</param>
/// <param name="MenuItemId">The ID of the menu item that was added.</param>
/// <param name="Quantity">The quantity of the item that was added.</param>
public record ItemAddedToTeamCart(
    TeamCartId TeamCartId,
    TeamCartItemId TeamCartItemId,
    UserId AddedByUserId,
    MenuItemId MenuItemId,
    int Quantity) : DomainEventBase;
