using YummyZoom.SharedKernel; 
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class RoleAssignment : ValueObject
{
    public string RoleName { get; private set; }
    public string? TargetEntityId { get; private set; } // Optional String ID
    public string? TargetEntityType { get; private set; } // Optional String

    private RoleAssignment(
        string roleName,
        string? targetEntityId,
        string? targetEntityType)
    {
        RoleName = roleName;
        TargetEntityId = targetEntityId;
        TargetEntityType = targetEntityType;
    }

    public static Result<RoleAssignment> Create(
        string roleName,
        string? targetEntityId = null,
        string? targetEntityType = null)
    {
        // Basic validation for roleName handled in Application layer.
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Result.Failure<RoleAssignment>(UserErrors.InvalidRoleName);
        }

        // Domain invariant: If TargetEntityId is provided, TargetEntityType must also be provided, and vice versa.
        if ((!string.IsNullOrWhiteSpace(targetEntityId) && string.IsNullOrWhiteSpace(targetEntityType)) ||
            (string.IsNullOrWhiteSpace(targetEntityId) && !string.IsNullOrWhiteSpace(targetEntityType)))
        {
             return Result.Failure<RoleAssignment>(UserErrors.InvalidRoleTarget);
        }

        return Result.Success(new RoleAssignment(
            roleName,
            targetEntityId,
            targetEntityType));
    }

    private static readonly object NullPlaceholder = new object();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoleName;
        yield return TargetEntityId ?? NullPlaceholder;
        yield return TargetEntityType ?? NullPlaceholder;
    }

#pragma warning disable CS8618
    // For EF Core
    private RoleAssignment()
    {
    }
#pragma warning restore CS8618
}
