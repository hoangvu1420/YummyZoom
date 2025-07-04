namespace YummyZoom.Domain.ReviewAggregate.ValueObjects;

public sealed class ReviewId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private ReviewId(Guid value)
    {
        Value = value;
    }

    public static ReviewId CreateUnique()
    {
        return new ReviewId(Guid.NewGuid());
    }

    public static ReviewId Create(Guid value)
    {
        return new ReviewId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private ReviewId()
    {
    }
#pragma warning restore CS8618
}
