using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;

public sealed class InitiateMemberOnlinePaymentCommandValidator : AbstractValidator<InitiateMemberOnlinePaymentCommand>
{
    public InitiateMemberOnlinePaymentCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();
    }
}


