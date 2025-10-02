namespace YummyZoom.Application.Notifications.Commands.SendNotificationToUser;

public class SendNotificationToUserCommandValidator : AbstractValidator<SendNotificationToUserCommand>
{
    public SendNotificationToUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Notification title is required.")
            .MaximumLength(100)
            .WithMessage("Notification title cannot exceed 100 characters.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Notification body is required.")
            .MaximumLength(500)
            .WithMessage("Notification body cannot exceed 500 characters.");

        RuleForEach(x => x.DataPayload!.Keys)
            .NotEmpty()
            .WithMessage("Data payload keys cannot be empty.")
            .When(x => x.DataPayload != null && x.DataPayload.Any());

        RuleForEach(x => x.DataPayload!.Values)
            .NotEmpty()
            .WithMessage("Data payload values cannot be empty.")
            .When(x => x.DataPayload != null && x.DataPayload.Any());
    }
}
