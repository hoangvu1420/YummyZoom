namespace YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;

public sealed class UpdateMenuItemDietaryTagsCommandValidator : AbstractValidator<UpdateMenuItemDietaryTagsCommand>
{
    public UpdateMenuItemDietaryTagsCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");

        RuleFor(v => v.DietaryTagIds)
            .Must(HaveUniqueItems).WithMessage("Dietary tag IDs must be unique.")
            .Must(HaveValidGuids).WithMessage("All dietary tag IDs must be valid GUIDs.")
            .When(v => v.DietaryTagIds != null);

        RuleForEach(v => v.DietaryTagIds)
            .NotEmpty().WithMessage("Dietary tag ID cannot be empty.")
            .When(v => v.DietaryTagIds != null);
    }

    private static bool HaveUniqueItems(List<Guid>? tagIds)
    {
        if (tagIds == null || tagIds.Count == 0)
            return true;
        return tagIds.Count == tagIds.Distinct().Count();
    }

    private static bool HaveValidGuids(List<Guid>? tagIds)
    {
        if (tagIds == null || tagIds.Count == 0)
            return true;
        return tagIds.All(id => id != Guid.Empty);
    }
}

