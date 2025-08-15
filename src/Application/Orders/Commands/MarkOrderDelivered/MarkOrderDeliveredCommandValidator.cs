namespace YummyZoom.Application.Orders.Commands.MarkOrderDelivered;

public sealed class MarkOrderDeliveredCommandValidator : AbstractValidator<MarkOrderDeliveredCommand>
{
    public MarkOrderDeliveredCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantGuid).NotEmpty();
        RuleFor(x => x.DeliveredAtUtc)
            .Must(dt => dt is null || dt <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("DeliveredAtUtc cannot be far in the future.");
    }
}
