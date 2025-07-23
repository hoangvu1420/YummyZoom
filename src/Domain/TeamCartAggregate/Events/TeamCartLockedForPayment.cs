using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a team cart is locked for payment.
/// This event signifies the transition from the Open state to the Locked state.
/// </summary>
public sealed record TeamCartLockedForPayment(
    TeamCartId TeamCartId,
    UserId HostUserId) : IDomainEvent;
