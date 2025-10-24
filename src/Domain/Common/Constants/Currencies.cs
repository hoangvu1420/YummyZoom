namespace YummyZoom.Domain.Common.Constants;

public static class Currencies
{
    public const string USD = "USD";
    public const string VND = "VND";
    // Future currencies can be added here
    // public const string EUR = "EUR";
    // public const string GBP = "GBP";

    public static string Default => VND;
}
