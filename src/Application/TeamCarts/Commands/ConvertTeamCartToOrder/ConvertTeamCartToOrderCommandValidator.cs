using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;

public sealed class ConvertTeamCartToOrderCommandValidator : AbstractValidator<ConvertTeamCartToOrderCommand>
{
    public ConvertTeamCartToOrderCommandValidator()
    {
        RuleFor(x => x.TeamCartId).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}


