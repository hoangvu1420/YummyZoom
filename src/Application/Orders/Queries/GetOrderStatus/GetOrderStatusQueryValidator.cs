using FluentValidation;

namespace YummyZoom.Application.Orders.Queries.GetOrderStatus;

/// <summary>
/// Validator for <see cref="GetOrderStatusQuery"/>.
/// </summary>
public sealed class GetOrderStatusQueryValidator : AbstractValidator<GetOrderStatusQuery>
{
    public GetOrderStatusQueryValidator()
    {
        RuleFor(x => x.OrderIdGuid)
            .NotEmpty().WithMessage("OrderId is required.");
    }
}
