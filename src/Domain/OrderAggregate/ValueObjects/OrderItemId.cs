namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class OrderItemId : ValueObject
{
    public Guid Value { get; private set; }

    private OrderItemId(Guid value)
    {
        Value = value;
    }

    public static OrderItemId CreateUnique()
    {
        return new OrderItemId(Guid.NewGuid());
    }

    public static OrderItemId Create(Guid value)
    {
        return new OrderItemId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private OrderItemId() { }
#pragma warning restore CS8618
}
