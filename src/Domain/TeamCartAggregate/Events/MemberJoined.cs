using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a new member joins a team cart.
/// </summary>
public sealed record MemberJoined(
    TeamCartId TeamCartId,
    UserId UserId,
    string Name) : DomainEventBase;
