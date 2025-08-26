namespace YummyZoom.Application.Restaurants.Queries.GetFullMenu;

public sealed class GetFullMenuQueryValidator : AbstractValidator<GetFullMenuQuery>
{
    public GetFullMenuQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");
    }
}
