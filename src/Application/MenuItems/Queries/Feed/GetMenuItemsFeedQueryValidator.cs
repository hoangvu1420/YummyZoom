using FluentValidation;

namespace YummyZoom.Application.MenuItems.Queries.Feed;

public sealed class GetMenuItemsFeedQueryValidator : AbstractValidator<GetMenuItemsFeedQuery>
{
    public GetMenuItemsFeedQueryValidator()
    {
        RuleFor(x => x.Tab)
            .NotEmpty()
            .Must(t => string.Equals(t.Trim(), "popular", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Tab must be one of: popular.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50);
    }
}

