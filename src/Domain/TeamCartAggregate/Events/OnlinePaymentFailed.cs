using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when an online payment fails.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="UserId">The ID of the user whose payment failed.</param>
/// <param name="Amount">The amount that failed to be paid.</param>
public record OnlinePaymentFailed(
    TeamCartId TeamCartId,
    UserId UserId,
    Money Amount) : DomainEventBase;
