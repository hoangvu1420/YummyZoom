namespace YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;

/// <summary>
/// Validates pagination and restaurant identifier for GetRestaurantActiveOrders.
/// </summary>
public sealed class GetRestaurantActiveOrdersQueryValidator : AbstractValidator<GetRestaurantActiveOrdersQuery>
{
    private const int MaxPageSize = 100;

    public GetRestaurantActiveOrdersQueryValidator()
    {
        RuleFor(x => x.RestaurantGuid)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"PageSize must be <= {MaxPageSize}.");
    }
}
