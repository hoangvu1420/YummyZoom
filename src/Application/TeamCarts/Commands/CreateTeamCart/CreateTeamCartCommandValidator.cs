using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;

public sealed class CreateTeamCartCommandValidator : AbstractValidator<CreateTeamCartCommand>
{
    public CreateTeamCartCommandValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty();

        RuleFor(x => x.HostName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.DeadlineUtc)
            .Must(d => d is null || d > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future");
    }
}

