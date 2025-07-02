namespace YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

public sealed class AccountTransactionId : ValueObject
{
    public Guid Value { get; private set; }

    private AccountTransactionId(Guid value)
    {
        Value = value;
    }

    public static AccountTransactionId CreateUnique()
    {
        return new AccountTransactionId(Guid.NewGuid());
    }

    public static AccountTransactionId Create(Guid value)
    {
        return new AccountTransactionId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private AccountTransactionId() { }
#pragma warning restore CS8618
}
