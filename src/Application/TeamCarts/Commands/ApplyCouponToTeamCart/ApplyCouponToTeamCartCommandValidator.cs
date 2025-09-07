using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;

public sealed class ApplyCouponToTeamCartCommandValidator : AbstractValidator<ApplyCouponToTeamCartCommand>
{
    public ApplyCouponToTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId).NotEmpty();
        RuleFor(x => x.CouponCode)
            .NotEmpty()
            .Must(code => !string.IsNullOrWhiteSpace(code))
            .WithMessage("Coupon code must not be empty or whitespace.")
            .MaximumLength(50);
    }
}


