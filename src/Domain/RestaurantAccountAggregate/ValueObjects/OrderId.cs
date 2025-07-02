namespace YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

// TODO: This is a placeholder until the Order aggregate is implemented
public sealed class OrderId : ValueObject
{
    public Guid Value { get; private set; }

    private OrderId(Guid value)
    {
        Value = value;
    }

    public static OrderId Create(Guid value)
    {
        return new OrderId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private OrderId() { }
#pragma warning restore CS8618
}
