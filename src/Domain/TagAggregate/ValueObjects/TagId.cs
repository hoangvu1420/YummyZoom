namespace YummyZoom.Domain.TagAggregate.ValueObjects;

public sealed class TagId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TagId(Guid value)
    {
        Value = value;
    }

    public static TagId CreateUnique()
    {
        return new TagId(Guid.NewGuid());
    }

    public static TagId Create(Guid value)
    {
        return new TagId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private TagId() { }
#pragma warning restore CS8618
}
