namespace YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;

public class DeleteRoleAssignmentValidator : AbstractValidator<DeleteRoleAssignmentCommand>
{
    public DeleteRoleAssignmentValidator()
    {
        RuleFor(v => v.RoleAssignmentId)
            .NotEmpty().WithMessage("RoleAssignmentId is required.");
    }
}
