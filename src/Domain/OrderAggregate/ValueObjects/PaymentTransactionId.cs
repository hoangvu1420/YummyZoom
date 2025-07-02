namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class PaymentTransactionId : ValueObject
{
    public Guid Value { get; private set; }

    private PaymentTransactionId(Guid value)
    {
        Value = value;
    }

    public static PaymentTransactionId CreateUnique()
    {
        return new PaymentTransactionId(Guid.NewGuid());
    }

    public static PaymentTransactionId Create(Guid value)
    {
        return new PaymentTransactionId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private PaymentTransactionId() { }
#pragma warning restore CS8618
}
