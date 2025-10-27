# MVP Centralized Pricing Constants Design

**Date:** 2025-01-27  
**Author:** Backend Team  
**Status:** Design Phase  
**Scope:** MVP - Simple centralized static values

## Problem Statement

Currently, pricing configuration values (tax rates, delivery fees) are scattered throughout the codebase as hardcoded constants, leading to:

- **Inconsistency**: Different values in different places (2.99m vs 15000m for delivery fees)
- **Maintenance Issues**: Changes require updates in multiple locations
- **Testing Complexity**: Hardcoded values make testing different scenarios difficult

## MVP Solution: Centralized Static Constants

For MVP scope, we'll create a simple centralized service with static constants, no database persistence, and minimal domain complexity.

### 1. Domain Layer - Simple Static Service

#### 1.1 Core Static Pricing Service

```csharp
// src/Domain/Services/StaticPricingService.cs
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
    public static readonly Money DefaultDeliveryFee = new Money(2.99m, "USD");

    /// <summary>
    /// Default tax rate for all restaurants (MVP)
    /// </summary>
    public static readonly decimal DefaultTaxRate = 0.08m; // 8%

    /// <summary>
    /// Default tax base policy (MVP)
    /// </summary>
    public static readonly TaxBasePolicy DefaultTaxBasePolicy = TaxBasePolicy.SubtotalAndFeesAndTip;

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
```

### 2. Application Layer Integration

#### 2.1 Enhanced Order Financial Service

```csharp
// src/Domain/Services/OrderFinancialService.cs (Enhanced for MVP)
public class OrderFinancialService
{
    private readonly ILogger<OrderFinancialService> _logger;

    public OrderFinancialService(ILogger<OrderFinancialService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the final total amount with centralized static pricing.
    /// </summary>
    public Money CalculateFinalTotalWithStaticPricing(
        RestaurantId restaurantId,
        Money subtotal,
        Money discount,
        Money tip)
    {
        var pricingConfig = StaticPricingService.GetPricingConfiguration(restaurantId);

        // Calculate tax based on policy
        var taxBase = CalculateTaxBase(subtotal, pricingConfig.DeliveryFee, tip, pricingConfig.TaxBasePolicy);
        var taxAmount = new Money(taxBase.Amount * pricingConfig.TaxRate, subtotal.Currency);

        // Calculate final total
        var finalTotal = subtotal - discount + pricingConfig.DeliveryFee + tip + taxAmount;
        
        return finalTotal;
    }

    private static Money CalculateTaxBase(Money subtotal, Money deliveryFee, Money tip, TaxBasePolicy policy)
    {
        return policy switch
        {
            TaxBasePolicy.SubtotalAndFeesAndTip => subtotal + deliveryFee + tip,
            TaxBasePolicy.SubtotalAndFees => subtotal + deliveryFee,
            TaxBasePolicy.SubtotalOnly => subtotal,
            _ => subtotal + deliveryFee + tip
        };
    }

    // Existing methods remain unchanged...
    public virtual Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems) { /* existing implementation */ }
    public virtual Result<Money> ValidateAndCalculateDiscount(Coupon coupon, IReadOnlyList<OrderItem> orderItems, Money subtotal, DateTime? currentTime = null) { /* existing implementation */ }
    public virtual Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax) { /* existing implementation */ }
}
```

#### 2.2 Updated Command Handlers

```csharp
// src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs (Updated for MVP)
public class InitiateOrderCommandHandler : IRequestHandler<InitiateOrderCommand, Result<InitiateOrderResponse>>
{
    // ... existing dependencies ...

    public async Task<Result<InitiateOrderResponse>> Handle(InitiateOrderCommand request, CancellationToken cancellationToken)
    {
        // ... existing validation and item building logic ...

        // 6. Calculate fees and taxes using centralized static pricing
        var tipAmount = new Money(request.TipAmount ?? 0m, currency);
        var pricingConfig = StaticPricingService.GetPricingConfiguration(restaurantId);
        
        var deliveryFee = pricingConfig.DeliveryFee;
        var taxBase = CalculateTaxBase(subtotal, deliveryFee, tipAmount, pricingConfig.TaxBasePolicy);
        var taxAmount = new Money(taxBase.Amount * pricingConfig.TaxRate, currency);

        // 7. Calculate final total using enhanced financial service
        var totalAmount = _orderFinancialService.CalculateFinalTotalWithStaticPricing(
            restaurantId, subtotal, discountAmount, tipAmount);

        // ... rest of existing logic ...
    }

    private static Money CalculateTaxBase(Money subtotal, Money deliveryFee, Money tip, TaxBasePolicy policy)
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
```

#### 2.3 Updated TeamCart Command Handlers

```csharp
// Example: src/Application/TeamCarts/Commands/LockTeamCartForPayment/LockTeamCartForPaymentCommandHandler.cs (Updated for MVP)
public class LockTeamCartForPaymentCommandHandler : IRequestHandler<LockTeamCartForPaymentCommand, Result>
{
    // ... existing dependencies ...

    public async Task<Result> Handle(LockTeamCartForPaymentCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // ... existing validation logic ...

            // Use centralized static pricing instead of hardcoded values
            var pricingConfig = StaticPricingService.GetPricingConfiguration(cart.RestaurantId);
            var feesTotal = pricingConfig.DeliveryFee; // No longer hardcoded 2.99m

            // ... rest of existing logic ...
        }, cancellationToken);
    }
}
```

### 3. Updated P1.2 Pricing Preview Implementation

#### 3.1 Updated Query Handler

```csharp
// src/Application/Pricing/Queries/GetPricingPreview/GetPricingPreviewQueryHandler.cs (Updated for MVP)
public class GetPricingPreviewQueryHandler : IRequestHandler<GetPricingPreviewQuery, Result<GetPricingPreviewResponse>>
{
    // ... existing dependencies ...

    public async Task<Result<GetPricingPreviewResponse>> Handle(
        GetPricingPreviewQuery request, 
        CancellationToken cancellationToken)
    {
        // ... existing validation and item building logic ...

        // 6. Get pricing configuration using centralized static service
        var pricingConfig = StaticPricingService.GetPricingConfiguration(restaurant.Id);
        
        var tipAmount = new Money(request.TipAmount ?? 0m, subtotal.Currency);
        var deliveryFee = pricingConfig.DeliveryFee;
        
        // Calculate tax based on policy
        var taxBase = CalculateTaxBase(subtotal, deliveryFee, tipAmount, pricingConfig.TaxBasePolicy);
        var taxAmount = new Money(taxBase.Amount * pricingConfig.TaxRate, subtotal.Currency);

        // 7. Calculate final total using enhanced financial service
        var finalTotal = _orderFinancialService.CalculateFinalTotalWithStaticPricing(
            restaurant.Id, subtotal, discountAmount ?? Money.Zero(subtotal.Currency), tipAmount);

        return Result.Success(new GetPricingPreviewResponse(
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            finalTotal,
            subtotal.Currency,
            notes,
            DateTime.UtcNow
        ));
    }

    private static Money CalculateTaxBase(Money subtotal, Money deliveryFee, Money tip, TaxBasePolicy policy)
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
```

### 4. Migration Strategy

#### 4.1 Simple Code Migration

**Phase 1: Create Static Service (1 day)**
- Create `StaticPricingService` class with centralized constants
- Add `TaxBasePolicy` enum and `StaticPricingConfiguration` record

**Phase 2: Update Existing Code (2-3 days)**
- Replace hardcoded values in `InitiateOrderCommandHandler`
- Update all TeamCart command handlers
- Update `OrderFinancialService` with new static pricing method
- Update seeding logic to use centralized constants

**Phase 3: Update P1.2 Pricing Preview (1 day)**
- Integrate with `StaticPricingService`
- Remove hardcoded values from preview implementation

**Phase 4: Testing (1 day)**
- Update tests to use centralized constants
- Validate consistency across all handlers

**Total Estimated Effort**: 4-5 development days

### 5. Benefits of MVP Approach

#### 5.1 Simplicity
- **No Database Complexity**: No new tables, migrations, or persistence logic
- **No Domain Complexity**: Simple static service with constants
- **Easy Testing**: Static values are easy to test and mock

#### 5.2 Consistency
- **Single Source of Truth**: All pricing constants in one place
- **Consistent Values**: Same pricing logic applied everywhere
- **Easy Maintenance**: Changes in one place affect entire system

#### 5.3 Future-Ready
- **Extensible Design**: Easy to enhance with per-restaurant logic later
- **Clean Interface**: Simple API that can be replaced with database-backed service
- **Backward Compatible**: No breaking changes to existing functionality

### 6. Implementation Steps

#### 6.1 Create Static Service
```csharp
// Create src/Domain/Services/StaticPricingService.cs
// Add TaxBasePolicy enum and StaticPricingConfiguration record
```

#### 6.2 Update Command Handlers
```csharp
// Replace hardcoded values in:
// - InitiateOrderCommandHandler
// - All TeamCart command handlers
// - OrderFinancialService
// - Seeding logic
```

#### 6.3 Update P1.2 Implementation
```csharp
// Update GetPricingPreviewQueryHandler to use StaticPricingService
// Remove hardcoded values from preview logic
```

#### 6.4 Update Tests
```csharp
// Update test constants to use StaticPricingService
// Validate consistency across all test scenarios
```

### 7. Future Enhancement Path

When ready to move beyond MVP, the static service can be easily replaced with:
- Database-backed configuration service
- Per-restaurant pricing rules
- Dynamic pricing based on context
- Regional pricing variations

The interface remains the same, making the transition seamless.

### 8. Conclusion

This MVP approach provides:
- **Immediate Benefits**: Centralized constants, consistency, easy maintenance
- **Minimal Complexity**: No database changes, simple static service
- **Future-Ready**: Easy to enhance when business requirements evolve
- **Quick Implementation**: 4-5 days vs 8-12 days for full solution

The design maintains all the benefits of centralization while keeping the implementation simple and focused on the immediate needs of the MVP scope.
