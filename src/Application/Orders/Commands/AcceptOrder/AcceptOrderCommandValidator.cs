namespace YummyZoom.Application.Orders.Commands.AcceptOrder;

public class AcceptOrderCommandValidator : AbstractValidator<AcceptOrderCommand>
{
    public AcceptOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.EstimatedDeliveryTime)
            .Must(dt => dt > DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Estimated delivery time must be at least 5 minutes in the future.")
            .Must(dt => dt < DateTime.UtcNow.AddHours(12))
            .WithMessage("Estimated delivery time cannot be more than 12 hours ahead.");
    }
}
