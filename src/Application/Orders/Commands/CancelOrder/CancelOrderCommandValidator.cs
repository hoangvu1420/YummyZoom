namespace YummyZoom.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantGuid).NotEmpty();
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
