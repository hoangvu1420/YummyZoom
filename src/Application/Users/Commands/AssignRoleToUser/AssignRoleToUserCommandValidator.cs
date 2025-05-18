using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.RoleName)
            .NotEmpty()
            .Must(roleName => typeof(Roles).GetFields().Any(f => f.IsLiteral && !f.IsInitOnly && f.GetValue(null)!.ToString() == roleName))
            .WithMessage("Invalid role name.");

        // If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.
        RuleFor(v => v)
            .Must(v =>
                (string.IsNullOrWhiteSpace(v.TargetEntityId) && string.IsNullOrWhiteSpace(v.TargetEntityType)) ||
                (!string.IsNullOrWhiteSpace(v.TargetEntityId) && !string.IsNullOrWhiteSpace(v.TargetEntityType)))
            .WithMessage("If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.");
    }
}
