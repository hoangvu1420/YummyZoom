using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when pricing (tip and coupon) is finalized for a team cart.
/// This event signifies the transition from the Locked state to the Finalized state.
/// After this point, tip and coupon become immutable and members can commit payments.
/// </summary>
public sealed record TeamCartPricingFinalized(
    TeamCartId TeamCartId,
    UserId HostUserId) : DomainEventBase;
