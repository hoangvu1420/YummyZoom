using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;

/// <summary>
/// RestaurantId value object. This is a placeholder until the Restaurant aggregate is implemented.
/// This represents a reference to a Restaurant aggregate by ID only.
/// </summary>
public sealed class RestaurantId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private RestaurantId(Guid value)
    {
        Value = value;
    }

    public static RestaurantId CreateUnique()
    {
        return new RestaurantId(Guid.NewGuid());
    }

    public static RestaurantId Create(Guid value)
    {
        return new RestaurantId(value);
    }

    public static Result<RestaurantId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<RestaurantId>(RoleAssignmentErrors.InvalidRestaurantId(value));
        }

        return Result.Success(new RestaurantId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private RestaurantId()
    {
    }
#pragma warning restore CS8618
}
