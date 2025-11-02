namespace YummyZoom.Application.Common.Currency;

public static class CurrencyMinorUnitConverter
{
    // Focused support for USD (2 decimals) and VND (0 decimals).
    // Defaults to 2 decimals if unknown.
    public static int GetDecimalPlaces(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return 2;

        switch (currency.Trim().ToUpperInvariant())
        {
            case "USD":
                return 2;
            case "VND":
                return 0;
            default:
                return 2;
        }
    }

    public static long ToMinorUnits(decimal majorAmount, string currency)
    {
        var decimals = GetDecimalPlaces(currency);
        var factor = decimals switch
        {
            0 => 1m,
            2 => 100m,
            3 => 1000m,
            _ => (decimal)Math.Pow(10, decimals)
        };

        return (long)Math.Round(majorAmount * factor, 0, MidpointRounding.AwayFromZero);
    }
}

