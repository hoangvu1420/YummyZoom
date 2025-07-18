namespace YummyZoom.Domain.TeamCartAggregate.Enums;

/// <summary>
/// Represents the method of payment in a team cart.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Payment made online via credit card, e-wallet, etc.
    /// </summary>
    Online,

    /// <summary>
    /// Payment to be made in cash upon delivery.
    /// </summary>
    CashOnDelivery
}