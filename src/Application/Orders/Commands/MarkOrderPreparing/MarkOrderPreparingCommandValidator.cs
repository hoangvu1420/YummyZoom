namespace YummyZoom.Application.Orders.Commands.MarkOrderPreparing;

public sealed class MarkOrderPreparingCommandValidator : AbstractValidator<MarkOrderPreparingCommand>
{
    public MarkOrderPreparingCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RestaurantGuid).NotEmpty();
    }
}
