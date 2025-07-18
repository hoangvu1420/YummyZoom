using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a team cart member commits to a payment method.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="UserId">The ID of the user making the payment commitment.</param>
/// <param name="Method">The payment method committed to.</param>
/// <param name="Amount">The amount committed to pay.</param>
public record MemberCommittedToPayment(
    TeamCartId TeamCartId,
    UserId UserId,
    PaymentMethod Method,
    Money Amount) : IDomainEvent;
