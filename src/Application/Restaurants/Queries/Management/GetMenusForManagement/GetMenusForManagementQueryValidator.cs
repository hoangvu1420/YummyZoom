namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;

public sealed class GetMenusForManagementQueryValidator : AbstractValidator<GetMenusForManagementQuery>
{
    public GetMenusForManagementQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");
    }
}

