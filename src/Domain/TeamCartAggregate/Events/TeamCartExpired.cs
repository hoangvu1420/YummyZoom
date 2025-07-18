using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a team cart expires.
/// </summary>
public sealed record TeamCartExpired(
    TeamCartId TeamCartId) : IDomainEvent;