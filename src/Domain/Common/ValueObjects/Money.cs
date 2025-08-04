namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            // In a real domain, this should be a custom exception. For now, ArgumentNull is fine.
            throw new ArgumentNullException(nameof(currency), "Currency cannot be null or empty.");
        }
        Amount = amount;
        Currency = currency;
    }
    
    /// <summary>
    /// Creates a new instance of Money with the same amount and currency.
    /// This ensures each entity has its own instance of Money value object.
    /// </summary>
    /// <returns>A new Money instance with the same values.</returns>
    public Money Copy() => new Money(Amount, Currency);

    public static Money Zero(string currency) => new Money(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot add money with different currencies.");
        }
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies.");
        }
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

#pragma warning disable CS8618
    private Money() { }
#pragma warning restore CS8618
}

public static class MoneyExtensions
{
    public static Money Sum<T>(this IEnumerable<T> source, Func<T, Money> selector, string currency)
    {
        return source.Select(selector).Aggregate(Money.Zero(currency), (current, next) => 
        {
            // Convert next to the target currency before adding
            var nextInTargetCurrency = new Money(next.Amount, currency);
            return current + nextInTargetCurrency;
        });
    }
}
