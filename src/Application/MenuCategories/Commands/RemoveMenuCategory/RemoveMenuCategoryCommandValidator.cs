namespace YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;

public class RemoveMenuCategoryCommandValidator : AbstractValidator<RemoveMenuCategoryCommand>
{
    public RemoveMenuCategoryCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuCategoryId)
            .NotEmpty()
            .WithMessage("Menu Category ID is required.");
    }
}
