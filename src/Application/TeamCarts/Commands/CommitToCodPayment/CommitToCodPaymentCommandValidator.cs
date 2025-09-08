using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;

public sealed class CommitToCodPaymentCommandValidator : AbstractValidator<CommitToCodPaymentCommand>
{
    public CommitToCodPaymentCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();
    }
}


