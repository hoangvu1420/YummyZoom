namespace YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;

public class ChangeMenuAvailabilityCommandValidator : AbstractValidator<ChangeMenuAvailabilityCommand>
{
    public ChangeMenuAvailabilityCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuId)
            .NotEmpty()
            .WithMessage("Menu ID is required.");
    }
}
