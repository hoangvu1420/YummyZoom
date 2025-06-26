namespace YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

public sealed class CustomizationGroupId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private CustomizationGroupId(Guid value)
    {
        Value = value;
    }

    public static CustomizationGroupId CreateUnique()
    {
        return new CustomizationGroupId(Guid.NewGuid());
    }

    public static CustomizationGroupId Create(Guid value)
    {
        return new CustomizationGroupId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private CustomizationGroupId() { }
#pragma warning restore CS8618
}
