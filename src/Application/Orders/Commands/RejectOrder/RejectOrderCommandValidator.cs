namespace YummyZoom.Application.Orders.Commands.RejectOrder;

public sealed class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
{
    public RejectOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantGuid).NotEmpty();
        RuleFor(x => x.RejectionReason)
            .MaximumLength(500)
            .WithMessage("Rejection reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.RejectionReason));
    }
}
