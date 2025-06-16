using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;

public sealed class RoleAssignmentId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private RoleAssignmentId(Guid value)
    {
        Value = value;
    }

    public static RoleAssignmentId CreateUnique()
    {
        return new RoleAssignmentId(Guid.NewGuid());
    }

    public static RoleAssignmentId Create(Guid value)
    {
        return new RoleAssignmentId(value);
    }

    public static Result<RoleAssignmentId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<RoleAssignmentId>(RoleAssignmentErrors.InvalidRoleAssignmentId(value));
        }

        return Result.Success(new RoleAssignmentId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private RoleAssignmentId()
    {
    }
#pragma warning restore CS8618
}
