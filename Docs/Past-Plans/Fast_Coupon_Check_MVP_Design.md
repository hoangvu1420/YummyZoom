# Fast Coupon Check — MVP Design & Implementation

**Version:** 1.0 (MVP)  
**Date:** October 23, 2025  
**Status:** Ready for Implementation

## Executive Summary

This document outlines a pragmatic MVP design for Fast Coupon Check that delivers core value quickly while setting up for future enhancements. The approach uses minimal new infrastructure, leverages existing patterns, and includes smart optimizations where they provide high ROI.

## 1. MVP Objectives

### Core Features
- **Individual Order Check**: Show applicable coupons for a cart with savings amounts
- **TeamCart Integration**: Provide coupon suggestions for collaborative orders
- **Best Deal Highlighting**: Surface the highest-value coupon prominently
- **Eligibility Clarity**: Show why coupons are/aren't applicable (min order, expiry, etc.)

### Success Criteria
- ✅ API response time <500ms (acceptable for MVP)
- ✅ Mathematical consistency with `OrderFinancialService`
- ✅ Works with existing authorization and validation patterns
- ✅ Supports 50+ active coupons per restaurant
- ✅ Zero breaking changes to existing coupon system

### Non-Goals (Future Phases)
- ❌ Sub-100ms response times (optimization phase)
- ❌ Complex multi-level caching (can add later)
- ❌ Real-time coupon notifications (TeamCart enhancement)
- ❌ Coupon stacking/combinations (advanced feature)

## 2. Architecture Overview

### 2.1 System Design

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Client Apps   │───▶│  Fast Check API  │───▶│  Query Handler  │
│ (Web/Mobile/RT) │    │   Endpoints      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                         │
                                ▼                         ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │  Authorization   │    │ Coupon Service  │
                       │   (Existing)     │    │ (New, Simple)   │
                       └──────────────────┘    └─────────────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │ Smart Projection│
                                                │ + Direct Query  │
                                                └─────────────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │ Existing Tables │
                                                │ + New View      │
                                                └─────────────────┘
```

### 2.2 Key Components

#### A. Minimal Database Changes
We'll add **one simple projection** for performance:

**`ActiveCouponsView`** (Materialized View - refreshed periodically)
```sql
CREATE MATERIALIZED VIEW active_coupons_view AS
SELECT 
    c.id as coupon_id,
    c.restaurant_id,
    c.code,
    c.description,
    c.value_type,
    c.value_percentage_value,
    c.value_fixed_amount_value,
    c.value_fixed_amount_currency,
    c.value_free_item_value,
    c.applies_to_scope,
    c.applies_to_item_ids,
    c.applies_to_category_ids,
    c.min_order_amount_amount,
    c.min_order_amount_currency,
    c.validity_start_date,
    c.validity_end_date,
    c.is_enabled,
    c.total_usage_limit,
    c.usage_limit_per_user,
    c.current_total_usage_count
FROM coupons c
WHERE c.is_enabled = true 
  AND c.is_deleted = false
  AND c.validity_end_date >= NOW();

CREATE UNIQUE INDEX idx_active_coupons_view_id ON active_coupons_view (coupon_id);
CREATE INDEX idx_active_coupons_view_restaurant ON active_coupons_view (restaurant_id);
```

**Refresh Strategy**: Simple background job every 5 minutes (good enough for MVP)

#### B. New Application Components

**`FastCouponCheckService`** - Core business logic
```csharp
public interface IFastCouponCheckService
{
    Task<CouponSuggestionsResponse> GetSuggestionsAsync(
        RestaurantId restaurantId, 
        IReadOnlyList<CartItem> cartItems, 
        UserId userId, 
        CancellationToken ct = default);
}
```

**Query Handlers** - Following existing CQRS patterns
```csharp
// Individual order endpoint
public record FastCouponCheckQuery(
    RestaurantId RestaurantId,
    IReadOnlyList<CartItem> Items,
    UserId UserId) : IRequest<CouponSuggestionsResponse>;

// TeamCart endpoint  
public record TeamCartCouponSuggestionsQuery(
    TeamCartId TeamCartId,
    UserId UserId) : IRequest<CouponSuggestionsResponse>;
```

## 3. Implementation Details

### 3.1 Core Algorithm (Simple & Effective)

```csharp
public class FastCouponCheckService : IFastCouponCheckService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly OrderFinancialService _financialService;

    public async Task<CouponSuggestionsResponse> GetSuggestionsAsync(
        RestaurantId restaurantId, 
        IReadOnlyList<CartItem> cartItems, 
        UserId userId, 
        CancellationToken ct = default)
    {
        // 1. Load active coupons for restaurant (single query)
        var coupons = await LoadActiveCouponsAsync(restaurantId, ct);
        
        // 2. Load user usage counts (single query)
        var userUsages = await LoadUserUsageCountsAsync(userId, coupons.Select(c => c.Id), ct);
        
        // 3. Convert cart items to order items (reuse existing logic)
        var orderItems = MapCartItemsToOrderItems(cartItems);
        var subtotal = _financialService.CalculateSubtotal(orderItems);
        
        // 4. Calculate suggestions (in-memory, fast)
        var suggestions = new List<CouponSuggestion>();
        
        foreach (var coupon in coupons)
        {
            var suggestion = await EvaluateCouponAsync(coupon, orderItems, subtotal, userUsages, ct);
            if (suggestion != null)
                suggestions.Add(suggestion);
        }
        
        // 5. Sort by best deal first
        var sortedSuggestions = suggestions
            .OrderByDescending(s => s.IsEligible)
            .ThenByDescending(s => s.Savings)
            .ThenBy(s => s.ExpiresOn)
            .ToList();
            
        return new CouponSuggestionsResponse
        {
            BestDeal = sortedSuggestions.FirstOrDefault(s => s.IsEligible),
            Suggestions = sortedSuggestions,
            CartSummary = new CartSummary 
            { 
                Subtotal = subtotal.Amount, 
                Currency = subtotal.Currency,
                ItemCount = cartItems.Sum(i => i.Quantity)
            }
        };
    }
    
    private async Task<CouponSuggestion?> EvaluateCouponAsync(
        ActiveCoupon coupon, 
        IReadOnlyList<OrderItem> orderItems, 
        Money subtotal,
        Dictionary<CouponId, int> userUsages,
        CancellationToken ct)
    {
        // 1. Check basic eligibility (time, enabled, usage limits)
        if (!IsBasicallyEligible(coupon, userUsages)) 
            return null;
            
        // 2. Use existing OrderFinancialService for consistency
        var domainCoupon = MapToDomainCoupon(coupon); // Simple mapping
        var discountResult = _financialService.ValidateAndCalculateDiscount(
            domainCoupon, orderItems, subtotal);
            
        // 3. Create suggestion based on result
        return new CouponSuggestion
        {
            Code = coupon.Code,
            Label = coupon.Description,
            Savings = discountResult.IsSuccess ? discountResult.Value.Amount : 0,
            IsEligible = discountResult.IsSuccess,
            EligibilityReason = discountResult.IsFailure ? MapErrorToReason(discountResult.Error) : null,
            MinOrderGap = CalculateMinOrderGap(coupon, subtotal),
            ExpiresOn = coupon.ValidityEndDate,
            Scope = coupon.AppliesTo.Scope.ToString()
        };
    }
}
```

### 3.2 Database Queries (Optimized but Simple)

```csharp
// Single query to load active coupons
private async Task<List<ActiveCoupon>> LoadActiveCouponsAsync(RestaurantId restaurantId, CancellationToken ct)
{
    using var connection = await _dbFactory.CreateConnectionAsync(ct);
    
    const string sql = @"
        SELECT * FROM active_coupons_view 
        WHERE restaurant_id = @RestaurantId
        ORDER BY validity_end_date ASC, code ASC";
        
    var results = await connection.QueryAsync<ActiveCouponDto>(sql, new { RestaurantId = restaurantId.Value }, ct);
    return results.Select(MapToActiveCoupon).ToList();
}

// Single query for user usage counts
private async Task<Dictionary<CouponId, int>> LoadUserUsageCountsAsync(UserId userId, IEnumerable<CouponId> couponIds, CancellationToken ct)
{
    using var connection = await _dbFactory.CreateConnectionAsync(ct);
    
    const string sql = @"
        SELECT coupon_id, usage_count 
        FROM coupon_user_usages 
        WHERE user_id = @UserId AND coupon_id = ANY(@CouponIds)";
        
    var results = await connection.QueryAsync<(Guid CouponId, int UsageCount)>(sql, 
        new { UserId = userId.Value, CouponIds = couponIds.Select(id => id.Value).ToArray() }, ct);
        
    return results.ToDictionary(r => CouponId.Create(r.CouponId), r => r.UsageCount);
}
```

## 4. API Design

### 4.1 Endpoints

#### Individual Order Fast Check
```http
POST /api/v1/coupons/fast-check
Authorization: Bearer {token}
Content-Type: application/json

{
  "restaurantId": "uuid",
  "items": [
    {
      "menuItemId": "uuid",
      "menuCategoryId": "uuid",
      "quantity": 2,
      "unitPrice": 15.99
    }
  ]
}
```

#### TeamCart Coupon Suggestions
```http
GET /api/v1/team-carts/{teamCartId}/coupon-suggestions
Authorization: Bearer {token}
```

### 4.2 Response Format

```json
{
  "cartSummary": {
    "subtotal": 31.98,
    "currency": "USD", 
    "itemCount": 2
  },
  "bestDeal": {
    "code": "SAVE20",
    "label": "20% off your order",
    "savings": 6.40,
    "isEligible": true,
    "expiresOn": "2025-11-15T23:59:59Z"
  },
  "suggestions": [
    {
      "code": "SAVE20",
      "label": "20% off your order",
      "savings": 6.40,
      "isEligible": true,
      "eligibilityReason": null,
      "minOrderGap": 0,
      "expiresOn": "2025-11-15T23:59:59Z",
      "scope": "WholeOrder"
    },
    {
      "code": "FREEDRINK", 
      "label": "Free drink with order",
      "savings": 0,
      "isEligible": false,
      "eligibilityReason": "MinAmountNotMet",
      "minOrderGap": 8.02,
      "expiresOn": "2025-12-01T23:59:59Z",
      "scope": "SpecificItems"
    }
  ]
}
```

## 5. TeamCart Integration

### 5.1 Simple Integration Pattern

```csharp
public class TeamCartCouponSuggestionsQueryHandler : IRequestHandler<TeamCartCouponSuggestionsQuery, CouponSuggestionsResponse>
{
    public async Task<CouponSuggestionsResponse> Handle(TeamCartCouponSuggestionsQuery request, CancellationToken ct)
    {
        // 1. Get TeamCart (existing pattern)
        var teamCart = await _teamCartRepository.GetByIdAsync(request.TeamCartId, ct);
        if (teamCart == null) 
            return CouponSuggestionsResponse.Empty();
            
        // 2. Convert TeamCart items to cart items
        var cartItems = teamCart.Items.Select(item => new CartItem
        {
            MenuItemId = item.MenuItemId,
            MenuCategoryId = item.MenuCategoryId, 
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice // Already calculated with customizations
        }).ToList();
        
        // 3. Use existing fast check service
        return await _fastCouponCheckService.GetSuggestionsAsync(
            teamCart.RestaurantId, cartItems, request.UserId, ct);
    }
}
```

## 6. Implementation Plan

### Phase 1: Foundation (Week 1)
- [ ] Create `ActiveCouponsView` materialized view
- [ ] Add refresh job for materialized view
- [ ] Implement `FastCouponCheckService` core logic
- [ ] Add unit tests for calculation consistency

### Phase 2: API Layer (Week 2)
- [ ] Create query handlers following CQRS pattern
- [ ] Add API endpoints with proper validation
- [ ] Implement authorization using existing patterns
- [ ] Add integration tests

### Phase 3: TeamCart Integration (Week 3)
- [ ] Add TeamCart coupon suggestions endpoint
- [ ] Test integration with existing TeamCart flows
- [ ] Add functional tests for end-to-end scenarios
- [ ] Performance testing and optimization

### Phase 4: Polish & Deploy (Week 4)
- [ ] Add logging and basic metrics
- [ ] Documentation and API specs
- [ ] Deployment and monitoring setup
- [ ] User acceptance testing

## 7. Testing Strategy

### 7.1 Unit Tests
- **Calculation Consistency**: Verify `FastCouponCheckService` produces same results as `OrderFinancialService`
- **Edge Cases**: Expired coupons, usage limits, min order amounts
- **Mapping Logic**: Cart items to order items conversion

### 7.2 Integration Tests
- **Database Queries**: Materialized view refresh and query performance
- **API Endpoints**: Full request/response validation
- **Authorization**: Proper access control enforcement

### 7.3 Functional Tests
- **End-to-End**: Complete user journey from cart to coupon suggestions
- **TeamCart Flow**: Collaborative ordering with coupon recommendations
- **Error Scenarios**: Invalid data, missing coupons, network issues

## 8. Monitoring & Operations

### 8.1 Key Metrics
```csharp
// Performance
- fast_coupon_check_duration_ms
- fast_coupon_check_requests_total
- materialized_view_refresh_duration_ms

// Business
- coupons_suggested_per_request
- best_deal_acceptance_rate
- api_usage_by_endpoint
```

### 8.2 Operational Tasks
- **Materialized View Refresh**: Every 5 minutes via background job
- **Performance Monitoring**: Track query times and optimize slow queries
- **Error Alerting**: Failed requests, database connectivity issues

## 9. Future Enhancements

### 9.1 Performance Optimizations (Phase 2)
- Add Redis caching for restaurant coupon bundles
- Implement smarter refresh strategies for materialized view
- Add query result caching for identical carts

### 9.2 Feature Enhancements (Phase 3+)
- Real-time coupon notifications in TeamCart
- Personalized coupon recommendations
- Coupon stacking and combination rules
- Advanced eligibility rules and targeting

## 10. Risk Mitigation

### 10.1 Performance Risks
- **Mitigation**: Start with materialized view, add caching if needed
- **Fallback**: Direct table queries if view refresh fails

### 10.2 Consistency Risks  
- **Mitigation**: Reuse `OrderFinancialService` for all calculations
- **Testing**: Comprehensive comparison tests between fast check and actual application

### 10.3 Complexity Risks
- **Mitigation**: Keep MVP simple, add complexity incrementally
- **Monitoring**: Track performance and add optimizations based on real usage

## Conclusion

This MVP design delivers core coupon checking functionality quickly while maintaining:

- ✅ **Simplicity**: Minimal new infrastructure and components
- ✅ **Consistency**: Reuses existing calculation logic
- ✅ **Performance**: Smart materialized view for common queries
- ✅ **Extensibility**: Clear path for future enhancements
- ✅ **Reliability**: Follows established patterns and practices

The approach balances speed of delivery with smart optimizations, providing immediate value while setting up for future growth.
