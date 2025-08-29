namespace YummyZoom.Application.Menus.Commands.UpdateMenuDetails;

public class UpdateMenuDetailsCommandValidator : AbstractValidator<UpdateMenuDetailsCommand>
{
    public UpdateMenuDetailsCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuId)
            .NotEmpty()
            .WithMessage("Menu ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters.");
    }
}
