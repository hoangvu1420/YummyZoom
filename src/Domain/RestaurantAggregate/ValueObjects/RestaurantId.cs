namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private RestaurantId() { }
#pragma warning restore CS8618
}
