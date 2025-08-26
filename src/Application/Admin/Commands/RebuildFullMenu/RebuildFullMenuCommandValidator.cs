namespace YummyZoom.Application.Admin.Commands.RebuildFullMenu;

public sealed class RebuildFullMenuCommandValidator : AbstractValidator<RebuildFullMenuCommand>
{
    public RebuildFullMenuCommandValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");
    }
}


