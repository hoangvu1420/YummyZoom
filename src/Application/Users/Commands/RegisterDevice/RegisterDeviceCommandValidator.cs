namespace YummyZoom.Application.Users.Commands.RegisterDevice;

public class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(v => v.FcmToken)
            .NotEmpty().WithMessage("FCM token is required.")
            .MaximumLength(512).WithMessage("FCM token must not exceed 512 characters.");

        RuleFor(v => v.Platform)
            .NotEmpty().WithMessage("Platform is required.")
            .MaximumLength(50).WithMessage("Platform must not exceed 50 characters.");

        RuleFor(v => v.DeviceId)
            .MaximumLength(100).WithMessage("Device ID must not exceed 100 characters.")
            .When(v => !string.IsNullOrWhiteSpace(v.DeviceId));
    }
}
