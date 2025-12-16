using FluentValidation;

namespace YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;

public sealed class SearchMenuItemsQueryValidator : AbstractValidator<SearchMenuItemsQuery>
{
    public SearchMenuItemsQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be >= 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be >= 1.");
    }
}
