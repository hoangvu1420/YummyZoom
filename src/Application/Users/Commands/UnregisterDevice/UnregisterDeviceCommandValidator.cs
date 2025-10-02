namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

public class UnregisterDeviceCommandValidator : AbstractValidator<UnregisterDeviceCommand>
{
    public UnregisterDeviceCommandValidator()
    {
        RuleFor(v => v.FcmToken)
            .NotEmpty().WithMessage("FCM token is required.")
            .MaximumLength(512).WithMessage("FCM token must not exceed 512 characters.");
    }
}
