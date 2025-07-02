using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    public static readonly Money Zero = new Money(0, "USD");
    
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>(Errors.MoneyErrors.InvalidCurrency);
        }

        return Result.Success(new Money(amount, currency));
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
#pragma warning disable CS8618
    private Money() { }
#pragma warning restore CS8618
}
