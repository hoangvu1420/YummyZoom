using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a member's role is updated in a team cart.
/// </summary>
public sealed record MemberRoleUpdated(
    TeamCartId TeamCartId,
    UserId UserId,
    MemberRole NewRole) : IDomainEvent;
