using YummyZoom.SharedKernel;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;

namespace YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

public sealed class RestaurantAccountId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private RestaurantAccountId(Guid value)
    {
        Value = value;
    }

    public static RestaurantAccountId CreateUnique()
    {
        return new RestaurantAccountId(Guid.NewGuid());
    }

    public static RestaurantAccountId Create(Guid value)
    {
        return new RestaurantAccountId(value);
    }

    public static Result<RestaurantAccountId> Create(string value)
    {
        return !Guid.TryParse(value, out var guid) 
            ? Result.Failure<RestaurantAccountId>(RestaurantAccountErrors.InvalidId(value))
            : Result.Success(new RestaurantAccountId(guid));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private RestaurantAccountId() { }
#pragma warning restore CS8618
}
