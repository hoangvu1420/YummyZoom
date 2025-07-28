using FluentValidation;
using YummyZoom.Domain.OrderAggregate.Enums;

namespace YummyZoom.Application.Orders.Commands.InitiateOrder;

public class InitiateOrderCommandValidator : AbstractValidator<InitiateOrderCommand>
{
    public InitiateOrderCommandValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required.")
            .Must(items => items.Count <= 50)
            .WithMessage("Maximum 50 items allowed per order.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemDtoValidator());

        RuleFor(x => x.DeliveryAddress)
            .NotNull()
            .WithMessage("Delivery address is required.")
            .SetValidator(new DeliveryAddressDtoValidator());

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50)
            .WithMessage("Coupon code cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.CouponCode));

        RuleFor(x => x.TipAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Tip amount must be non-negative.")
            .When(x => x.TipAmount.HasValue);

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("Special instructions cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SpecialInstructions));
    }
}

public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemDtoValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty()
            .WithMessage("Menu item ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(10)
            .WithMessage("Maximum quantity per item is 10.");
    }
}

public class DeliveryAddressDtoValidator : AbstractValidator<DeliveryAddressDto>
{
    public DeliveryAddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street is required.")
            .MaximumLength(200)
            .WithMessage("Street cannot exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .MaximumLength(100)
            .WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required.")
            .MaximumLength(100)
            .WithMessage("State cannot exceed 100 characters.");

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .WithMessage("Zip code is required.")
            .MaximumLength(20)
            .WithMessage("Zip code cannot exceed 20 characters.");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required.")
            .MaximumLength(100)
            .WithMessage("Country cannot exceed 100 characters.");
    }
}
