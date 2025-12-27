using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.PayoutAggregate.ValueObjects;

public sealed class PayoutId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private PayoutId(Guid value)
    {
        Value = value;
    }

    public static PayoutId CreateUnique()
    {
        return new PayoutId(Guid.NewGuid());
    }

    public static PayoutId Create(Guid value)
    {
        return new PayoutId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private PayoutId() { }
#pragma warning restore CS8618
}
