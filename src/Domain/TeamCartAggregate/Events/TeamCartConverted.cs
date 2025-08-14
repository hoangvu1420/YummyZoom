using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a TeamCart is successfully converted to an Order.
/// This event signals the completion of the collaborative ordering process.
/// </summary>
public record TeamCartConverted(
    TeamCartId TeamCartId,
    OrderId OrderId,
    DateTime ConvertedAt,
    UserId ConvertedByUserId) : DomainEventBase;
