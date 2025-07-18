using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Events;

/// <summary>
/// Domain event raised when a team cart is ready for confirmation (all payments complete/committed).
/// </summary>
/// <param name="TeamCartId">The ID of the team cart.</param>
/// <param name="TotalAmount">The total amount of all payments.</param>
/// <param name="CashAmount">The total amount to be paid in cash on delivery.</param>
public record TeamCartReadyForConfirmation(
    TeamCartId TeamCartId,
    Money TotalAmount,
    Money CashAmount) : IDomainEvent;
