using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.Services;

/// <summary>
/// Static pricing service for MVP - provides centralized access to pricing constants.
/// No database persistence, simple static values for consistency.
/// </summary>
public static class StaticPricingService
{
    /// <summary>
    /// Default delivery fee for all restaurants (MVP)
    /// </summary>
    public static readonly Money DefaultDeliveryFee = new Money(15000m, "VND");

    /// <summary>
    /// Default tax rate for all restaurants (MVP)
    /// </summary>
    public static readonly decimal DefaultTaxRate = 0.08m; // 8%

    /// <summary>
    /// Default tax base policy (MVP)
    /// </summary>
    public static readonly TaxBasePolicy DefaultTaxBasePolicy = TaxBasePolicy.SubtotalOnly;

    /// <summary>
    /// Gets the delivery fee for MVP (currently static for all restaurants)
    /// </summary>
    public static Money GetDeliveryFee(RestaurantId restaurantId)
    {
        // MVP: Return static value for all restaurants
        // Future: Can be enhanced with per-restaurant logic
        return DefaultDeliveryFee;
    }

    /// <summary>
    /// Gets the tax rate for MVP (currently static for all restaurants)
    /// </summary>
    public static decimal GetTaxRate(RestaurantId restaurantId)
    {
        // MVP: Return static value for all restaurants
        // Future: Can be enhanced with per-restaurant logic
        return DefaultTaxRate;
    }

    /// <summary>
    /// Gets the tax base policy for MVP (currently static for all restaurants)
    /// </summary>
    public static TaxBasePolicy GetTaxBasePolicy(RestaurantId restaurantId)
    {
        // MVP: Return static value for all restaurants
        // Future: Can be enhanced with per-restaurant logic
        return DefaultTaxBasePolicy;
    }

    /// <summary>
    /// Gets all pricing constants for a restaurant in a single call (MVP)
    /// </summary>
    public static StaticPricingConfiguration GetPricingConfiguration(RestaurantId restaurantId)
    {
        return new StaticPricingConfiguration(
            DeliveryFee: DefaultDeliveryFee,
            TaxRate: DefaultTaxRate,
            TaxBasePolicy: DefaultTaxBasePolicy
        );
    }

    /// <summary>
    /// Calculates the tax base amount based on the tax policy.
    /// </summary>
    public static Money CalculateTaxBase(Money subtotal, Money deliveryFee, Money tip, TaxBasePolicy policy)
    {
        return policy switch
        {
            TaxBasePolicy.SubtotalAndFeesAndTip => subtotal + deliveryFee + tip,
            TaxBasePolicy.SubtotalAndFees => subtotal + deliveryFee,
            TaxBasePolicy.SubtotalOnly => subtotal,
            _ => subtotal + deliveryFee + tip
        };
    }
}

/// <summary>
/// Tax base policy configuration (simple enum for MVP)
/// </summary>
public enum TaxBasePolicy
{
    /// <summary>
    /// Tax applies to subtotal + delivery fee + tip
    /// </summary>
    SubtotalAndFeesAndTip,
    
    /// <summary>
    /// Tax applies to subtotal + delivery fee only
    /// </summary>
    SubtotalAndFees,
    
    /// <summary>
    /// Tax applies to subtotal only
    /// </summary>
    SubtotalOnly
}

/// <summary>
/// Static pricing configuration for MVP
/// </summary>
public record StaticPricingConfiguration(
    Money DeliveryFee,
    decimal TaxRate,
    TaxBasePolicy TaxBasePolicy
);
