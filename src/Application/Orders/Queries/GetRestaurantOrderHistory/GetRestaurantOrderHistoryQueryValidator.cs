namespace YummyZoom.Application.Orders.Queries.GetRestaurantOrderHistory;

/// <summary>
/// Validates pagination and filters for GetRestaurantOrderHistory.
/// </summary>
public sealed class GetRestaurantOrderHistoryQueryValidator : AbstractValidator<GetRestaurantOrderHistoryQuery>
{
    private const int MaxPageSize = 100;

    public GetRestaurantOrderHistoryQueryValidator()
    {
        RuleFor(x => x.RestaurantGuid)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"PageSize must be <= {MaxPageSize}.");

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From must be <= To.");
    }
}
