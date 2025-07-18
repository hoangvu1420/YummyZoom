using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when an online payment is successfully completed.
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="UserId">The ID of the user who made the payment.</param>
/// <param name="TransactionId">The transaction ID from the payment processor.</param>
/// <param name="Amount">The amount that was paid.</param>
public record OnlinePaymentSucceeded(
    TeamCartId TeamCartId,
    UserId UserId,
    string TransactionId,
    Money Amount) : IDomainEvent;
