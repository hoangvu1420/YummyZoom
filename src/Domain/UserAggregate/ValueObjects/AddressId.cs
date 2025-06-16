namespace YummyZoom.Domain.UserAggregate.ValueObjects;

public sealed class AddressId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private AddressId(Guid value)
    {
        Value = value;
    }

    public static AddressId CreateUnique()
    {
        return new AddressId(Guid.NewGuid());
    }

    public static AddressId Create(Guid value)
    {
        return new AddressId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private AddressId()
    {
    }
#pragma warning restore CS8618
}
