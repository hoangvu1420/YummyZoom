# OrderFinancialService Unit Tests Outline

Based on my analysis of the <mcfile name="OrderFinancialService.cs" path="e:\source\repos\CA\YummyZoom\src\Domain\Services\OrderFinancialService.cs"></mcfile> and the existing test patterns in the project, here's a comprehensive test outline:

## Test File Structure

```
tests/Domain.UnitTests/Services/OrderFinancialServiceTests/
├── OrderFinancialServiceTestsBase.cs
├── CalculateSubtotalTests.cs
├── ValidateAndCalculateDiscountTests/
│   ├── ValidateAndCalculateDiscountSuccessTests.cs
│   ├── ValidateAndCalculateDiscountFailureTests.cs
│   └── ValidateAndCalculateDiscountEdgeCaseTests.cs
└── CalculateFinalTotalTests.cs
```

## Test Classes Overview

### 1. OrderFinancialServiceTestsBase.cs

- **Purpose**: Base class providing common setup, test data builders, and helper methods
- **Key Components**:
  - Service instantiation (real implementation, no mocks)
  - Common test data factories for OrderItems, Coupons, Money objects
  - Helper methods for creating test scenarios
  - Assertion helpers for financial calculations

### 2. CalculateSubtotalTests.cs

- **Test Scenarios**:
  - Empty order items list (should return Money.Zero with default currency)
  - Single order item calculation
  - Multiple order items with same currency
  - Order items with customizations affecting line totals
  - Different quantities and prices
  - Decimal precision handling

### 3. ValidateAndCalculateDiscountSuccessTests.cs

- **Test Scenarios**:
  - **Percentage Coupons**: 10%, 25%, 50%, 100% discounts
  - **Fixed Amount Coupons**: Various amounts, ensuring discount doesn't exceed applicable base
  - **Free Item Coupons**: Single free item, multiple quantities of free item
  - **Scope Testing**:
    - WholeOrder scope with various order compositions
    - SpecificItems scope with matching and non-matching items
    - SpecificCategories scope with matching categories
  - **Usage Limits**: Valid usage within limits
  - **Time Validity**: Active coupons within valid date range
  - **Minimum Order Amount**: Orders meeting minimum requirements

### 4. ValidateAndCalculateDiscountFailureTests.cs

- **Test Scenarios**:
  - **Coupon State Failures**:
    - Disabled coupons
    - Expired coupons
    - Future-dated coupons (not yet valid)
  - **Usage Limit Failures**:
    - Total usage limit exceeded
    - Per-user usage limit exceeded
  - **Order Condition Failures**:
    - Minimum order amount not met
    - No applicable items in scope
  - **Invalid Coupon Types**: Unknown/invalid coupon types
  - **Free Item Not Found**: Free item coupon when item not in order

### 5. ValidateAndCalculateDiscountEdgeCaseTests.cs

- **Test Scenarios**:
  - **Boundary Conditions**:
    - Exactly at minimum order amount
    - Exactly at usage limits
    - Discount equals full applicable amount
  - **Currency Handling**: Different currencies between coupon and order
  - **Precision**: Decimal rounding scenarios
  - **Custom Time**: Testing with specific DateTime values
  - **Complex Scope Scenarios**: Mixed item/category applicability

### 6. CalculateFinalTotalTests.cs

- **Test Scenarios**:
  - **Basic Calculations**: Subtotal + fees + tax - discount
  - **Zero Values**: Zero discount, zero fees, zero tax
  - **Negative Total Prevention**: Ensuring total never goes below zero
  - **Large Discount Scenarios**: Discount larger than subtotal
  - **Currency Consistency**: All Money objects with same currency
  - **Decimal Precision**: Proper rounding and precision handling

## Key Test Data Builders

### OrderItemTestHelpers

- `CreateOrderItem(price, quantity, menuItemId, categoryId)`
- `CreateOrderItemWithCustomizations(basePrice, customizations)`
- `CreateMultipleOrderItems(count, basePrice)`

### CouponTestHelpers (extend existing)

- `CreateValidPercentageCoupon(percentage, scope, restrictions)`
- `CreateValidFixedAmountCoupon(amount, scope, restrictions)`
- `CreateValidFreeItemCoupon(itemId, scope)`
- `CreateExpiredCoupon()`, `CreateFutureCoupon()`, `CreateDisabledCoupon()`
- `CreateCouponWithUsageLimits(totalLimit, userLimit)`
- `CreateCouponWithMinOrderAmount(minAmount)`

## Testing Patterns to Follow

1. **Arrange-Act-Assert**: Clear separation in all tests
2. **Result Pattern Testing**: Use <mcsymbol name="ResultAssertions" filename="ResultAssertions.cs" path="e:\source\repos\CA\YummyZoom\tests\Domain.UnitTests\ResultAssertions.cs" startline="7" type="class"></mcsymbol> for consistent assertions
3. **Descriptive Test Names**: `MethodName_Scenario_ExpectedResult` format
4. **Test Categories**: Use `[TestFixture]` and group related scenarios
5. **Edge Case Coverage**: Boundary conditions, null handling, invalid inputs
6. **Business Rule Validation**: Each business rule should have corresponding test cases
7. **Real Dependencies**: Use actual OrderFinancialService instance, no mocking

## Coverage Goals

- **Line Coverage**: 100% of OrderFinancialService methods
- **Branch Coverage**: All conditional paths and business rules
- **Edge Cases**: Boundary conditions and error scenarios
- **Integration**: Proper interaction with domain value objects
- **Business Rules**: Every coupon validation rule and calculation logic

This comprehensive test suite will ensure the OrderFinancialService is thoroughly tested, maintainable, and follows the established patterns in the YummyZoom project.
