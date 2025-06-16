namespace YummyZoom.Application.RoleAssignments.Commands.UpdateRoleAssignment;

public class UpdateRoleAssignmentValidator : AbstractValidator<UpdateRoleAssignmentCommand>
{
    public UpdateRoleAssignmentValidator()
    {
        RuleFor(v => v.RoleAssignmentId)
            .NotEmpty().WithMessage("RoleAssignmentId is required.");

        RuleFor(v => v.NewRole)
            .IsInEnum().WithMessage("NewRole must be a valid RestaurantRole value.");
    }
}
