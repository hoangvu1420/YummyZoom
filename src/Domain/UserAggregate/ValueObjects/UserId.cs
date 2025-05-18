using YummyZoom.Domain.UserAggregate.Errors; // Assuming UserErrors will be in this namespace
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class UserId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId CreateUnique()
    {
        return new UserId(Guid.NewGuid());
    }

    public static UserId Create(Guid value)
    {
        return new UserId(value);
    }

    public static Result<UserId> Create(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            // Assuming UserErrors class will have an InvalidUserId error
            return Result.Failure<UserId>(UserErrors.InvalidUserId(value));
        }

        return Result.Success(new UserId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private UserId()
    {
    }
#pragma warning restore CS8618
}
