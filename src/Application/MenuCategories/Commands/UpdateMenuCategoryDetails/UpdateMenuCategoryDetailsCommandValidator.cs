namespace YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;

public class UpdateMenuCategoryDetailsCommandValidator : AbstractValidator<UpdateMenuCategoryDetailsCommand>
{
    public UpdateMenuCategoryDetailsCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuCategoryId)
            .NotEmpty()
            .WithMessage("Menu Category ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.DisplayOrder)
            .GreaterThan(0)
            .WithMessage("Display order must be greater than zero.");
    }
}
