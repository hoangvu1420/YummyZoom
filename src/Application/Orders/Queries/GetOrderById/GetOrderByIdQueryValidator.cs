namespace YummyZoom.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
    RuleFor(x => x.OrderIdGuid)
            .NotEmpty().WithMessage("OrderId is required.");
    }
}
