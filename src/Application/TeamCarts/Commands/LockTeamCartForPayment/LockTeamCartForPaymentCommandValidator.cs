using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

public sealed class LockTeamCartForPaymentCommandValidator : AbstractValidator<LockTeamCartForPaymentCommand>
{
    public LockTeamCartForPaymentCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();
    }
}

