namespace YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;

public sealed class MarkOrderReadyForDeliveryCommandValidator : AbstractValidator<MarkOrderReadyForDeliveryCommand>
{
    public MarkOrderReadyForDeliveryCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantGuid).NotEmpty();
    }
}
