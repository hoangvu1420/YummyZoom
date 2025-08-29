namespace YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;

public class AddMenuCategoryCommandValidator : AbstractValidator<AddMenuCategoryCommand>
{
    public AddMenuCategoryCommandValidator()
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
    }
}
