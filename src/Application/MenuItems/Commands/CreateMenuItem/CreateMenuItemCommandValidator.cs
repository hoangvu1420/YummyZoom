namespace YummyZoom.Application.MenuItems.Commands.CreateMenuItem;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
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

        RuleFor(v => v.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters.");

        RuleFor(v => v.Price)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Price must be greater than or equal to zero.");

        RuleFor(v => v.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-character ISO code (e.g., USD, EUR).")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters only (e.g., USD, EUR).");

        RuleFor(v => v.ImageUrl)
            .MaximumLength(500)
            .WithMessage("Image URL must not exceed 500 characters.")
            .Must(BeValidUrlWhenProvided)
            .WithMessage("Image URL must be a valid URL.")
            .When(v => !string.IsNullOrWhiteSpace(v.ImageUrl));

        RuleFor(v => v.DietaryTagIds)
            .Must(HaveUniqueItems)
            .WithMessage("Dietary tag IDs must be unique.")
            .Must(HaveValidGuids)
            .WithMessage("All dietary tag IDs must be valid GUIDs.")
            .When(v => v.DietaryTagIds?.Count > 0);

        RuleForEach(v => v.DietaryTagIds)
            .NotEmpty()
            .WithMessage("Dietary tag ID cannot be empty.")
            .When(v => v.DietaryTagIds?.Count > 0);
    }

    private static bool BeValidUrlWhenProvided(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
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
