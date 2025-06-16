namespace YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;

public class CreateRoleAssignmentValidator : AbstractValidator<CreateRoleAssignmentCommand>
{
    public CreateRoleAssignmentValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(v => v.Role)
            .IsInEnum().WithMessage("Role must be a valid RestaurantRole value.");
    }
}
