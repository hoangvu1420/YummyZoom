using FluentValidation;

namespace YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;

/// <summary>
/// Validates pagination parameters for <see cref="GetCustomerRecentOrdersQuery"/>.
/// </summary>
public sealed class GetCustomerRecentOrdersQueryValidator : AbstractValidator<GetCustomerRecentOrdersQuery>
{
    private const int MaxPageSize = 100;

    public GetCustomerRecentOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"PageSize must be <= {MaxPageSize}.");
    }
}
