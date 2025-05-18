namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Rating : ValueObject
{
    public Rating(int value)
    {
        Value = value;
    }

    public int Value { get; private set; }

    public static Rating Create(int value)
    {
        // TODO: Enforce invariants
        return new Rating(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
#pragma warning disable CS8618
    private Rating()
    {
    }
#pragma warning restore CS8618
}
