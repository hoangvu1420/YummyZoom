# InitiateOrder Command Tests Analysis and Outline

## Overview

This document provides a comprehensive analysis and test outline for the `InitiateOrder` command handler functional tests. The tests will focus specifically on the command logic and business rules, separate from the payment integration tests that already exist.

## Command Analysis

### Command Structure
The `InitiateOrderCommand` is the main entry point for order creation and includes:
- **Authentication**: Requires `[Authorize]` attribute - user must be authenticated
- **Input Parameters**:
  - `CustomerId` (Guid) - The customer placing the order
  - ### Test Implementation Guidelines

#### Test Approach Clarification
The `InitiateOrder` command logic tests are designed to complement, not duplicate, the existing payment integration tests:

- **Command Logic Tests** (New): Focus on business rules, validation, financial calculations, and command handling logic with mocked payment gateway
- **Payment Integration Tests** (Existing): Test complete payment flows with real Stripe service in `PaymentIntegration` folder

#### Test Data StrategystaurantId` (Guid) - Target restaurant for the order
  - `Items` (List<OrderItemDto>) - Menu items with quantities
  - `DeliveryAddress` (DeliveryAddressDto) - Complete delivery address
  - `PaymentMethod` (string) - Payment method type
  - `SpecialInstructions` (string, optional) - Customer notes
  - `CouponCode` (string, optional) - Discount coupon
  - `TipAmount` (decimal, optional) - Customer tip
  - `TeamCartId` (Guid, optional) - For group orders

### Business Logic Flow
The command handler executes the following key operations:
1. **Input Validation** - Validates payment method enum conversion
2. **Restaurant Validation** - Checks restaurant exists and is active
3. **Menu Item Validation** - Validates items exist, belong to restaurant, and are available
4. **Address Validation** - Creates DeliveryAddress value object
5. **Order Item Creation** - Creates OrderItem entities with current pricing
6. **Financial Calculations** - Calculates subtotal, taxes, fees, discounts
7. **Coupon Processing** - Validates and applies coupon if provided
8. **Payment Intent Creation** - Creates payment intent for online payments
9. **Order Creation** - Creates the Order aggregate
10. **Persistence** - Saves the order to database

### Dependencies
- `IOrderRepository` - Order persistence
- `IRestaurantRepository` - Restaurant validation
- `IMenuItemRepository` - Menu item validation and pricing
- `ICouponRepository` - Coupon validation and usage tracking
- `IPaymentGatewayService` - Payment intent creation
- `OrderFinancialService` - Financial calculations
- `IUnitOfWork` - Transaction management

### Error Scenarios
The command can fail with several specific errors:
- `RestaurantNotFound` - Restaurant doesn't exist
- `RestaurantNotActive` - Restaurant not accepting orders
- `MenuItemsNotFound` - One or more menu items not found
- `MenuItemsNotFromRestaurant` - Items don't belong to specified restaurant
- `MenuItemNotAvailable` - Item temporarily unavailable
- `InvalidPaymentMethod` - Payment method not recognized
- `DeliveryAddress validation errors` - Invalid address components
- `OrderItem creation errors` - Invalid quantities or pricing
- `Coupon validation errors` - Invalid, expired, or exceeded usage coupons
- `Payment gateway errors` - Payment intent creation failures

## Test Strategy

### Test Organization
Tests will be organized into the following categories:

#### 1. **Happy Path Tests** 
Test successful order creation scenarios with valid inputs.

#### 2. **Validation Tests**
Test FluentValidation rules and input validation.

#### 3. **Business Rule Tests**
Test domain business rules and constraints.

#### 4. **Error Handling Tests**
Test specific error scenarios and edge cases.

#### 5. **Integration Tests**
Test interactions with dependencies and services.

#### 6. **Authorization Tests**
Test authentication and authorization requirements.

### Test Data Strategy
- **Use Default Test Data**: Leverage existing `Testing.TestData` for restaurants, menu items, users, and coupons
- **Custom Test Entities**: Create specific test entities only when testing creation/validation logic
- **Data Isolation**: Each test starts with clean state via `BaseTestFixture`

## Test Folder Structure

The tests will be organized within the existing functional test structure with logical separation across multiple test files:

```
tests/Application.FunctionalTests/Features/Orders/
├── PaymentIntegration/               # Existing - Real Stripe integration tests
│   ├── OnlineOrderPaymentTests.cs   # End-to-end payment flow tests
│   └── PaymentTestHelper.cs         # Stripe-specific test utilities
└── Commands/                        # New - Command logic tests
    └── InitiateOrder/               # New folder for InitiateOrder command tests
        ├── InitiateOrderHappyPathTests.cs          # Happy path scenarios
        ├── InitiateOrderValidationTests.cs         # Validation and input tests
        ├── InitiateOrderBusinessRuleTests.cs       # Business rules and domain logic
        ├── InitiateOrderFinancialTests.cs          # Financial calculations
        ├── InitiateOrderPaymentTests.cs            # Payment gateway interactions
        ├── InitiateOrderAuthorizationTests.cs      # Authentication/authorization
        ├── InitiateOrderEdgeCaseTests.cs           # Edge cases and error handling
        ├── InitiateOrderTestBase.cs                # Base test setup class
        └── InitiateOrderTestHelper.cs              # Shared test utilities
```

### Test File Organization

Each test file focuses on a specific aspect of the command:

1. **`InitiateOrderHappyPathTests.cs`** (~6 tests)
   - Basic successful order creation scenarios
   - Different payment methods (COD, online payments)
   - Orders with coupons, tips, special instructions

2. **`InitiateOrderValidationTests.cs`** (~8 tests)
   - FluentValidation rule testing
   - Required field validation
   - Input format and constraint validation

3. **`InitiateOrderBusinessRuleTests.cs`** (~10 tests)
   - Restaurant validation (exists, active)
   - Menu item validation (exists, belongs to restaurant, available)
   - Coupon business rules (expiry, usage limits, conditions)

4. **`InitiateOrderFinancialTests.cs`** (~6 tests)
   - Subtotal calculations
   - Tax and fee calculations
   - Discount calculations
   - Total amount calculations

5. **`InitiateOrderPaymentTests.cs`** (~8 tests)
   - Payment intent creation for different methods
   - Payment gateway error handling
   - Metadata validation
   - Cash on delivery handling

6. **`InitiateOrderAuthorizationTests.cs`** (~3 tests)
   - Authentication requirements
   - User context validation

7. **`InitiateOrderEdgeCaseTests.cs`** (~5 tests)
   - Concurrent operations
   - Transaction consistency
   - Audit trails and domain events

### Test Helper Class Structure

The `InitiateOrderTestHelper` will provide utilities specific to command testing:

```csharp
namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Test helper class providing utilities for InitiateOrder command testing.
/// Focuses on command construction, mocking setup, and validation helpers.
/// </summary>
public static class InitiateOrderTestHelper
{
    #region Command Builders
    
    /// <summary>
    /// Builds a valid InitiateOrder command with default test data.
    /// </summary>
    public static InitiateOrderCommand BuildValidCommand(
        Guid? customerId = null,
        Guid? restaurantId = null,
        List<Guid>? menuItemIds = null,
        string paymentMethod = "CreditCard",
        string? couponCode = null,
        decimal? tipAmount = null,
        string? specialInstructions = null)
    
    /// <summary>
    /// Builds a command with invalid data for validation testing.
    /// </summary>
    public static InitiateOrderCommand BuildInvalidCommand(...)
    
    #endregion
    
    #region Mock Setup Helpers
    
    /// <summary>
    /// Configures IPaymentGatewayService mock for successful payment intent creation.
    /// </summary>
    public static Mock<IPaymentGatewayService> SetupSuccessfulPaymentGatewayMock()
    
    /// <summary>
    /// Configures IPaymentGatewayService mock to return failure.
    /// </summary>
    public static Mock<IPaymentGatewayService> SetupFailingPaymentGatewayMock()
    
    #endregion
    
    #region Assertion Helpers
    
    /// <summary>
    /// Validates that an order was created with expected financial calculations.
    /// </summary>
    public static void ValidateOrderFinancials(Order order, ...)
    
    /// <summary>
    /// Validates payment intent creation was called with correct parameters.
    /// </summary>
    public static void ValidatePaymentIntentCreation(Mock<IPaymentGatewayService> mock, ...)
    
    #endregion
    
    #region Test Data
    
    /// <summary>
    /// Default delivery address for testing.
    /// </summary>
    public static DeliveryAddressDto DefaultDeliveryAddress { get; }
    
    /// <summary>
    /// Common test payment methods.
    /// </summary>
    public static class PaymentMethods
    {
        public const string CreditCard = "CreditCard";
        public const string PayPal = "PayPal";
        public const string ApplePay = "ApplePay";
        public const string GooglePay = "GooglePay";
        public const string CashOnDelivery = "CashOnDelivery";
    }
    
    #endregion
}
```

## Detailed Test Outline

### Shared Test Setup and Helper

#### `InitiateOrderTestHelper.cs`
Common utilities shared across all test files:

```csharp
namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Shared test helper class providing utilities for all InitiateOrder command tests.
/// </summary>
public static class InitiateOrderTestHelper
{
    // ... (implementation details as previously outlined)
}
```

#### Base Test Setup Pattern
All test classes will follow this common setup pattern:

```csharp
public abstract class InitiateOrderTestBase : BaseTestFixture
{
    protected Mock<IPaymentGatewayService> PaymentGatewayMock { get; private set; } = null!;

    [SetUp]
    public virtual async Task SetUp()
    {
        // Set default customer as current user
        SetUserId(Testing.TestData.DefaultCustomerId);
        
        // Setup payment gateway mock for each test
        PaymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        
        // Replace the real payment gateway service with mock
        ReplaceService<IPaymentGatewayService>(PaymentGatewayMock.Object);
    }
}
```

---

#### 1. Happy Path Tests

##### 1.1 Basic Order Creation
**Test**: `InitiateOrder_WithValidData_ShouldCreateOrderSuccessfully`
- **Arrange**: 
  - Setup successful payment gateway mock
  - Valid command using InitiateOrderTestHelper.BuildValidCommand()
- **Act**: Send InitiateOrderCommand
- **Assert**: 
  - Result is successful
  - Order created with correct OrderId and OrderNumber
  - Total amount calculated correctly
  - Order persisted in database with correct properties
  - Payment gateway called with correct parameters

##### 1.2 Cash on Delivery Order
**Test**: `InitiateOrder_WithCashOnDelivery_ShouldCreateOrderWithoutPaymentIntent`
- **Arrange**: Valid command with CashOnDelivery payment method
- **Act**: Send command
- **Assert**:
  - Order created successfully
  - No PaymentIntentId or ClientSecret returned
  - Order status appropriate for COD

##### 1.3 Order with Valid Coupon
**Test**: `InitiateOrder_WithValidCoupon_ShouldApplyDiscountCorrectly`
- **Arrange**: Valid command with existing valid coupon code
- **Act**: Send command
- **Assert**:
  - Order created successfully
  - Discount amount applied correctly
  - Coupon usage count incremented
  - Final total reflects discount

##### 1.4 Order with Tip
**Test**: `InitiateOrder_WithTipAmount_ShouldIncludeTipInTotal`
- **Arrange**: Valid command with tip amount specified
- **Act**: Send command
- **Assert**:
  - Order created with correct tip amount
  - Total includes tip in calculation

##### 1.5 Order with Special Instructions
**Test**: `InitiateOrder_WithSpecialInstructions_ShouldStoreInstructions`
- **Arrange**: Valid command with special instructions
- **Act**: Send command
- **Assert**:
  - Order created with instructions stored correctly

##### 1.6 Multiple Menu Items Order
**Test**: `InitiateOrder_WithMultipleMenuItems_ShouldCalculateCorrectly`
- **Arrange**: Command with multiple different menu items and quantities
- **Act**: Send command
- **Assert**:
  - Order contains all items with correct quantities
  - Subtotal calculated correctly for all items
  - Order items have correct pricing snapshots

#### 2. Validation Tests

##### 2.1 Required Field Validation
**Test**: `InitiateOrder_WithMissingRestaurantId_ShouldFailValidation`
- **Arrange**: Command with empty RestaurantId
- **Act**: Send command
- **Assert**: ValidationException thrown

**Test**: `InitiateOrder_WithEmptyItems_ShouldFailValidation`
- **Arrange**: Command with empty Items list
- **Act**: Send command
- **Assert**: ValidationException thrown

**Test**: `InitiateOrder_WithMissingDeliveryAddress_ShouldFailValidation`
- **Arrange**: Command with null DeliveryAddress
- **Act**: Send command
- **Assert**: ValidationException thrown

##### 2.2 Item Quantity Validation
**Test**: `InitiateOrder_WithZeroQuantity_ShouldFailValidation`
- **Arrange**: Command with item quantity = 0
- **Act**: Send command
- **Assert**: ValidationException thrown

**Test**: `InitiateOrder_WithNegativeQuantity_ShouldFailValidation`
- **Arrange**: Command with negative item quantity
- **Act**: Send command
- **Assert**: ValidationException thrown

**Test**: `InitiateOrder_WithExcessiveQuantity_ShouldFailValidation`
- **Arrange**: Command with quantity > 10 (max allowed)
- **Act**: Send command
- **Assert**: ValidationException thrown

##### 2.3 Order Size Validation
**Test**: `InitiateOrder_WithTooManyItems_ShouldFailValidation`
- **Arrange**: Command with > 50 items (max allowed)
- **Act**: Send command
- **Assert**: ValidationException thrown

##### 2.4 Address Validation
**Test**: `InitiateOrder_WithInvalidDeliveryAddress_ShouldFailValidation`
- **Arrange**: Command with invalid address components (empty street, city, etc.)
- **Act**: Send command
- **Assert**: ValidationException thrown

##### 2.5 Payment Method Validation
**Test**: `InitiateOrder_WithInvalidPaymentMethod_ShouldFailValidation`
- **Arrange**: Command with invalid payment method string
- **Act**: Send command
- **Assert**: ValidationException thrown

##### 2.6 Optional Field Validation
**Test**: `InitiateOrder_WithTooLongSpecialInstructions_ShouldFailValidation`
- **Arrange**: Command with special instructions > 500 characters
- **Act**: Send command
- **Assert**: ValidationException thrown

**Test**: `InitiateOrder_WithNegativeTip_ShouldFailValidation`
- **Arrange**: Command with negative tip amount
- **Act**: Send command
- **Assert**: ValidationException thrown

#### 3. Business Rule Tests

##### 3.1 Restaurant Validation
**Test**: `InitiateOrder_WithNonExistentRestaurant_ShouldFailWithRestaurantNotFound`
- **Arrange**: Command with non-existent restaurant ID
- **Act**: Send command
- **Assert**: 
  - Result is failure
  - Error is RestaurantNotFound

**Test**: `InitiateOrder_WithInactiveRestaurant_ShouldFailWithRestaurantNotActive`
- **Arrange**: 
  - Create inactive restaurant
  - Command targeting inactive restaurant
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is RestaurantNotActive

##### 3.2 Menu Item Validation
**Test**: `InitiateOrder_WithNonExistentMenuItem_ShouldFailWithMenuItemsNotFound`
- **Arrange**: Command with non-existent menu item ID
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is MenuItemsNotFound

**Test**: `InitiateOrder_WithMenuItemsFromDifferentRestaurant_ShouldFailWithMenuItemsNotFromRestaurant`
- **Arrange**: 
  - Create second restaurant with menu items
  - Command with items from multiple restaurants
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is MenuItemsNotFromRestaurant

**Test**: `InitiateOrder_WithUnavailableMenuItem_ShouldFailWithMenuItemNotAvailable`
- **Arrange**: 
  - Mark menu item as unavailable
  - Command with unavailable item
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is MenuItemNotAvailable

##### 3.3 Coupon Validation
**Test**: `InitiateOrder_WithNonExistentCoupon_ShouldFailWithCouponNotFound`
- **Arrange**: Command with invalid coupon code
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is Coupon.NotFound

**Test**: `InitiateOrder_WithExpiredCoupon_ShouldFailWithCouponExpired`
- **Arrange**: 
  - Create expired coupon
  - Command with expired coupon code
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is CouponExpired

**Test**: `InitiateOrder_WithDisabledCoupon_ShouldFailWithCouponDisabled`
- **Arrange**: 
  - Create disabled coupon
  - Command with disabled coupon code
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is CouponDisabled

**Test**: `InitiateOrder_WithCouponExceedingUsageLimit_ShouldFailWithUsageLimitExceeded`
- **Arrange**: 
  - Create coupon with usage limit
  - Use coupon up to limit
  - Command attempting to exceed limit
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is UsageLimitExceeded

**Test**: `InitiateOrder_WithCouponBelowMinimumOrder_ShouldFailWithMinAmountNotMet`
- **Arrange**: 
  - Create coupon with minimum order amount
  - Command with order below minimum
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Error is MinAmountNotMet

#### 4. Financial Calculation Tests

##### 4.1 Subtotal Calculation
**Test**: `InitiateOrder_WithMultipleItems_ShouldCalculateSubtotalCorrectly`
- **Arrange**: Command with various menu items and quantities
- **Act**: Send command
- **Assert**: 
  - Order subtotal matches manual calculation
  - Individual line items calculated correctly

##### 4.2 Tax and Fee Calculation
**Test**: `InitiateOrder_ShouldCalculateTaxAndDeliveryFeeCorrectly`
- **Arrange**: Valid command
- **Act**: Send command
- **Assert**:
  - Tax amount calculated at correct rate (8%)
  - Delivery fee applied correctly ($2.99)
  - Total includes all components

##### 4.3 Discount Calculation
**Test**: `InitiateOrder_WithPercentageCoupon_ShouldCalculateDiscountCorrectly`
- **Arrange**: 
  - Create percentage discount coupon
  - Valid command with coupon
- **Act**: Send command
- **Assert**:
  - Discount amount calculated correctly
  - Final total reflects discount

**Test**: `InitiateOrder_WithFixedAmountCoupon_ShouldCalculateDiscountCorrectly`
- **Arrange**: 
  - Create fixed amount discount coupon
  - Valid command with coupon
- **Act**: Send command
- **Assert**:
  - Fixed discount amount applied
  - Final total correct

##### 4.4 Total Calculation
**Test**: `InitiateOrder_ShouldCalculateFinalTotalCorrectly`
- **Arrange**: Command with items, tip, and coupon
- **Act**: Send command
- **Assert**:
  - Final total = subtotal - discount + delivery fee + tip + tax
  - All components properly included

#### 5. Payment Integration Tests (Command Logic Focus)

##### 5.1 Online Payment Intent Creation
**Test**: `InitiateOrder_WithCreditCardPayment_ShouldCreatePaymentIntent`
- **Arrange**: 
  - Setup payment gateway mock to return successful payment intent
  - Command with CreditCard payment method
- **Act**: Send command
- **Assert**:
  - PaymentIntentId returned from response
  - ClientSecret returned from response
  - Payment gateway service called with correct parameters (amount, currency, metadata)
  - Order created with AwaitingPayment status
  - Order contains payment intent reference

**Test**: `InitiateOrder_WithPayPalPayment_ShouldCreatePaymentIntent`
- **Arrange**: 
  - Setup payment gateway mock for PayPal
  - Command with PayPal payment method
- **Act**: Send command
- **Assert**:
  - Payment intent created for PayPal
  - Correct payment method type recorded

**Test**: `InitiateOrder_WithApplePayPayment_ShouldCreatePaymentIntent`
- **Arrange**: 
  - Setup payment gateway mock for ApplePay
  - Command with ApplePay payment method
- **Act**: Send command
- **Assert**:
  - Payment intent created with ApplePay metadata

**Test**: `InitiateOrder_WithGooglePayPayment_ShouldCreatePaymentIntent`
- **Arrange**: 
  - Setup payment gateway mock for GooglePay
  - Command with GooglePay payment method
- **Act**: Send command
- **Assert**:
  - Payment intent created with GooglePay metadata

##### 5.2 Payment Gateway Error Handling
**Test**: `InitiateOrder_WhenPaymentGatewayFails_ShouldFailWithPaymentError`
- **Arrange**: 
  - Setup payment gateway mock to return failure
  - Valid command with online payment
- **Act**: Send command
- **Assert**:
  - Result is failure
  - Payment gateway error propagated
  - Order not created in database
  - Transaction rolled back

##### 5.3 Payment Intent Metadata Validation
**Test**: `InitiateOrder_ShouldIncludeOrderMetadataInPaymentIntent`
- **Arrange**: 
  - Setup payment gateway mock to capture parameters
  - Valid command with online payment
- **Act**: Send command
- **Assert**:
  - Payment gateway called with metadata containing user_id
  - Payment gateway called with metadata containing restaurant_id
  - Payment gateway called with metadata containing order_id
  - Metadata values match command parameters

##### 5.4 Cash on Delivery Handling
**Test**: `InitiateOrder_WithCashOnDelivery_ShouldNotCreatePaymentIntent`
- **Arrange**: Command with CashOnDelivery payment method
- **Act**: Send command
- **Assert**:
  - Order created successfully
  - No PaymentIntentId or ClientSecret returned
  - Payment gateway service not called
  - Order status appropriate for COD payment
  - No payment transaction record created

#### 6. Authorization Tests

##### 6.1 Authentication Required
**Test**: `InitiateOrder_WithoutAuthentication_ShouldFailWithUnauthorized`
- **Arrange**: 
  - Clear user context (no authentication)
  - Valid command
- **Act**: Send command
- **Assert**: UnauthorizedAccessException thrown

##### 6.2 Customer Context
**Test**: `InitiateOrder_AsAuthenticatedUser_ShouldSucceed`
- **Arrange**: 
  - Set authenticated user context
  - Valid command
- **Act**: Send command
- **Assert**: Order created successfully

#### 7. Edge Cases and Error Handling Tests

##### 7.1 Concurrent Operations
**Test**: `InitiateOrder_WithConcurrentCouponUsage_ShouldHandleCorrectly`
- **Arrange**: 
  - Coupon with limited usage
  - Multiple concurrent order requests
- **Act**: Send commands simultaneously
- **Assert**: Only allowed number of uses succeed

##### 7.2 Data Consistency and Transaction Management
**Test**: `InitiateOrder_ShouldMaintainTransactionConsistency`
- **Arrange**: 
  - Setup payment gateway mock for successful response
  - Valid command
- **Act**: Send command
- **Assert**:
  - Order and payment intent created atomically
  - Transaction boundaries maintained
  - Consistent state across all entities

**Test**: `InitiateOrder_WhenPaymentGatewayFailsAfterOrderCreation_ShouldRollbackTransaction`
- **Arrange**: 
  - Setup payment gateway mock to fail after some processing
  - Valid command with online payment
- **Act**: Send command
- **Assert**:
  - Result is failure
  - No order persisted in database
  - Transaction properly rolled back
  - No partial data saved

##### 7.3 Menu Item Pricing Snapshot
**Test**: `InitiateOrder_ShouldCaptureCurrentMenuItemPricing`
- **Arrange**: Valid command with menu items
- **Act**: Send command
- **Assert**:
  - Order items contain pricing snapshot
  - Prices match current menu item prices at time of order

##### 7.4 Order Number Generation
**Test**: `InitiateOrder_ShouldGenerateUniqueOrderNumber`
- **Arrange**: Multiple valid commands
- **Act**: Send commands sequentially
- **Assert**:
  - Each order has unique order number
  - Order numbers follow expected format

#### 8. Integration and Cross-Cutting Tests

##### 8.1 Audit Trail
**Test**: `InitiateOrder_ShouldSetCreationAuditFields`
- **Arrange**: Valid command with authenticated user
- **Act**: Send command
- **Assert**:
  - Created timestamp set
  - CreatedBy field populated with current user

##### 8.2 Domain Events
**Test**: `InitiateOrder_ShouldPublishOrderInitiatedEvent`
- **Arrange**: Valid command
- **Act**: Send command
- **Assert**:
  - Domain event published
  - Event contains correct order information

##### 8.3 Transaction Boundary
**Test**: `InitiateOrder_ShouldExecuteInTransaction`
- **Arrange**: Valid command
- **Act**: Send command
- **Assert**:
  - All operations executed within single transaction
  - Consistent state maintained

## Test Implementation Guidelines

### Test Data Strategy
1. **Leverage Default Test Data**: Use `Testing.TestData` properties for restaurants, menu items, users
2. **Minimal Custom Setup**: Only create custom entities when testing specific scenarios
3. **Consistent Addresses**: Use standard test addresses for delivery validation
4. **Valid Payment Methods**: Use supported payment method strings from validator

### Assertion Patterns
1. **Result Pattern**: Use `ShouldBeSuccessful()` and `ShouldBeFailure()` extensions
2. **Database Verification**: Use `FindAsync<Order>()` to verify persistence
3. **Value Object Equality**: Use `.Value` properties for ID comparisons
4. **Error Verification**: Check specific error codes and messages

### Mock Strategy
1. **Focused Mocking**: Mock `IPaymentGatewayService` to isolate command logic testing from payment integration complexity
2. **Real Database**: Use real database with Testcontainers for authentic data persistence testing
3. **Separation of Concerns**: 
   - **Command Logic Tests**: Mock payment gateway, focus on business rules and command handling
   - **Payment Integration Tests**: Use real Stripe service (existing tests in PaymentIntegration folder)
4. **Service Replacement**: Use `ReplaceService<T>()` method to substitute mocked services in test setup
5. **Mock Verification**: Verify mock interactions to ensure correct service calls with expected parameters
6. **Time Dependencies**: Use fixed time for coupon validation tests when testing time-sensitive scenarios

### Performance Considerations
1. **Parallel Test Execution**: Ensure tests are isolated and can run concurrently
2. **Database Reset**: Rely on Respawner for fast data cleanup
3. **Resource Management**: Properly dispose of resources in test cleanup

## Conclusion

This comprehensive test suite will provide high coverage of the `InitiateOrder` command logic, ensuring robust validation of business rules, error handling, and integration points. The tests follow established patterns in the codebase and leverage the existing test infrastructure for maintainability and reliability.

The test organization allows for easy maintenance and extension as the command evolves, while the detailed scenarios ensure that edge cases and error conditions are properly handled.
