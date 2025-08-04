# Order Payment Integration Tests - Analysis and Implementation Outline

## Project Context Analysis

Based on the analysis of the YummyZoom project structure and existing payment implementation, this document outlines the comprehensive integration testing strategy for the order payment process, including the two-phase payment flow with Stripe.

### Current Implementation Overview

**Architecture Pattern:**

- Clean Architecture with Domain-Driven Design (DDD)
- Application layer orchestrates business processes via MediatR commands
- Domain aggregates encapsulate business logic and invariants
- Infrastructure layer provides concrete implementations for external services

**Payment Flow:**

1. **Order Initiation**: `InitiateOrderCommand` creates order with `AwaitingPayment` status and Stripe PaymentIntent
2. **Payment Processing**: Frontend completes payment with Stripe using client secret
3. **Webhook Processing**: `HandleStripeWebhookCommand` processes Stripe events and updates order status

**Key Components:**

- `Order` aggregate with `RecordPaymentSuccess()` and `RecordPaymentFailure()` methods
- `IPaymentGatewayService` abstraction with `StripeService` implementation
- `CustomWebApplicationFactory` for functional testing infrastructure
- Existing webhook handling via `HandleStripeWebhookCommandHandler`

## Integration Test Structure and Organization

### Test File Organization

```
tests/Application.FunctionalTests/
├── Features/
│   └── Orders/
│       └── PaymentIntegration/
│           ├── OnlineOrderPaymentTests.cs
│           ├── PaymentWebhookTests.cs
│           └── PaymentSecurityTests.cs
└── Infrastructure/
    └── PaymentTestHelpers/
        ├── StripeTestHelper.cs
        └── PaymentTestDataBuilder.cs
```

### Base Test Infrastructure Requirements

**Test Configuration Extensions:**

- Extend `CustomWebApplicationFactory` to support Stripe test configuration
- Configure test Stripe API keys and webhook secrets
- Mock or configure test payment methods
- Ensure proper test database isolation

**Test Helper Classes:**

- `StripeTestHelper`: Utilities for creating test payment intents, simulating payments, and generating webhook payloads
- `PaymentTestDataBuilder`: Builder pattern for creating test orders with various payment scenarios
- `WebhookSignatureGenerator`: Helper for creating valid Stripe webhook signatures

## Test Cases Implementation Plan

### 1. OnlineOrderPaymentTests.cs

**Purpose**: End-to-end testing of the complete online payment flow

#### Test Case 1.1: Happy Path - Successful Payment Flow

```csharp
[Test]
public async Task InitiateOrder_WithOnlinePayment_WhenPaymentSucceeds_ShouldCompleteOrderSuccessfully()
```

**Test Flow:**

1. **Arrange**: Create valid `InitiateOrderCommand` with online payment method
2. **Act Part 1**: Send command to `/api/orders` endpoint via HTTP client
3. **Assert Part 1**: Verify response contains OrderId and PaymentIntentId
4. **Act Part 2**: Use Stripe SDK to confirm payment with test card `pm_card_visa`
5. **Act Part 3**: Generate and send `payment_intent.succeeded` webhook
6. **Assert Part 2**: Verify order status is `Placed` in database

#### Test Case 1.2: Payment Failure Flow

```csharp
[Test]
public async Task InitiateOrder_WithOnlinePayment_WhenPaymentFails_ShouldCancelOrder()
```

**Test Flow:**

1. **Arrange**: Create valid `InitiateOrderCommand`
2. **Act Part 1**: Create order and get PaymentIntentId
3. **Act Part 2**: Simulate payment failure with `pm_card_visa_chargeDeclined`
4. **Act Part 3**: Send `payment_intent.payment_failed` webhook
5. **Assert**: Verify order status is `Cancelled`

#### Test Case 1.3: Order Creation with COD Payment

```csharp
[Test]
public async Task InitiateOrder_WithCODPayment_ShouldCreateOrderDirectlyAsPlaced()
```

**Test Flow:**

1. **Arrange**: Create `InitiateOrderCommand` with COD payment method
2. **Act**: Send command to API
3. **Assert**: Verify order is created with `Placed` status immediately, no PaymentIntent created

### 2. PaymentWebhookTests.cs

**Purpose**: Testing webhook processing scenarios and edge cases

#### Test Case 2.1: Webhook Idempotency

```csharp
[Test]
public async Task HandleWebhook_WhenProcessedTwice_ShouldBeIdempotent()
```

**Test Flow:**

1. **Arrange**: Create order and valid webhook payload
2. **Act**: Send same webhook twice
3. **Assert**: Verify order is updated only once, second call returns success

#### Test Case 2.2: Webhook for Non-Existent Order

```csharp
[Test]
public async Task HandleWebhook_WithInvalidPaymentIntentId_ShouldLogAndReturnSuccess()
```

#### Test Case 2.3: Webhook Event Ordering

```csharp
[Test]
public async Task HandleWebhook_WhenEventsArriveOutOfOrder_ShouldHandleGracefully()
```

### 3. PaymentSecurityTests.cs

**Purpose**: Testing security aspects of payment processing

#### Test Case 3.1: Invalid Webhook Signature

```csharp
[Test]
public async Task HandleWebhook_WithInvalidSignature_ShouldReturnBadRequest()
```

**Test Flow:**

1. **Arrange**: Create valid webhook payload with invalid signature
2. **Act**: Send to webhook endpoint
3. **Assert**: Verify 400 Bad Request response

#### Test Case 3.2: Missing Stripe Signature Header

```csharp
[Test]
public async Task HandleWebhook_WithMissingSignatureHeader_ShouldReturnBadRequest()
```

#### Test Case 3.3: Malformed Webhook Payload

```csharp
[Test]
public async Task HandleWebhook_WithMalformedJson_ShouldReturnBadRequest()
```

## Test Infrastructure Implementation Details

### Test Configuration Strategy

**Recommended Approach**: Extend the existing `CustomWebApplicationFactory` rather than creating a separate factory, and use configuration-based approach for Stripe test secrets.

#### Option 2: User Secrets for CI/CD (Alternative)

For teams preferring user secrets approach:

**Initialize user secrets for test project:**

```bash
cd tests/Application.FunctionalTests
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_test_key_here"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_test_your_webhook_secret_here"
```

**Update test project file:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <UserSecretsId>YummyZoom-FunctionalTests-12345</UserSecretsId>
  </PropertyGroup>
</Project>
```

### StripeTestHelper Implementation

```csharp
public static class StripeTestHelper
{
    public static async Task<PaymentIntent> ConfirmPaymentAsync(string paymentIntentId, string paymentMethod)
    {
        var service = new PaymentIntentService();
        return await service.ConfirmAsync(paymentIntentId, new PaymentIntentConfirmOptions
        {
            PaymentMethod = paymentMethod
        });
    }
    
    public static string GenerateWebhookPayload(string eventType, string paymentIntentId)
    {
        // Generate realistic Stripe webhook JSON payload
    }
    
    public static string GenerateWebhookSignature(string payload, string secret)
    {
        // Use Stripe.Webhook.WebhookSignature.Generate()
    }
}
```

### Test Configuration Extensions

**Add to `TestConfiguration.cs`:**

```csharp
/// <summary>
/// Payment and Stripe test configuration.
/// </summary>
public static class Payment
{
    /// <summary>
    /// Test Stripe configuration section name.
    /// </summary>
    public const string StripeSectionName = "Stripe";
    
    /// <summary>
    /// Test payment methods for different scenarios.
    /// </summary>
    public static class TestPaymentMethods
    {
        /// <summary>
        /// Visa card that will succeed.
        /// </summary>
        public const string VisaSuccess = "pm_card_visa";
        
        /// <summary>
        /// Card that will be declined.
        /// </summary>
        public const string VisaDeclined = "pm_card_visa_chargeDeclined";
        
        /// <summary>
        /// Card that requires authentication.
        /// </summary>
        public const string VisaAuthentication = "pm_card_visa_chargeDeclinedInsufficientFunds";
    }
    
    /// <summary>
    /// Test webhook event types.
    /// </summary>
    public static class WebhookEvents
    {
        public const string PaymentIntentSucceeded = "payment_intent.succeeded";
        public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
        public const string PaymentIntentCanceled = "payment_intent.canceled";
    }
}
```

### Test Data Builders

```csharp
public class PaymentOrderTestDataBuilder
{
    public static InitiateOrderCommand BuildValidOnlineOrderCommand()
    {
        return new InitiateOrderCommand(
            CustomerId: Guid.NewGuid(),
            RestaurantId: Guid.NewGuid(),
            Items: new List<OrderItemDto> { /* test items */ },
            DeliveryAddress: new DeliveryAddressDto(/* test address */),
            PaymentMethod: "CreditCard",
            TipAmount: 5.00m
        );
    }
    
    public static InitiateOrderCommand BuildValidCODOrderCommand()
    {
        // Similar but with COD payment method
    }
}
```

## Test Execution Strategy

### Test Categories and Execution

1. **Unit Tests**: Fast, isolated tests for individual components
2. **Integration Tests**: Test component interactions within the application
3. **End-to-End Tests**: Full payment flow including external Stripe API calls

### Test Environment Configuration

- Use Stripe test environment with test API keys
- Configure test webhook endpoints
- Ensure test database isolation between test runs
- Mock external dependencies where appropriate

### Performance Considerations

- Minimize actual Stripe API calls where possible
- Use test fixtures to share expensive setup operations
- Implement proper test cleanup to avoid state leakage
- Consider parallel test execution limitations with external APIs

## Additional Test Scenarios

### Edge Cases and Error Handling

1. **Network Failures**: Test resilience when Stripe API is unavailable
2. **Timeout Scenarios**: Test behavior when payment confirmation times out
3. **Partial Failures**: Test scenarios where order creation succeeds but payment setup fails
4. **Concurrent Access**: Test multiple users attempting to order simultaneously
5. **Invalid Payment Methods**: Test handling of declined cards, expired cards, etc.

### Business Logic Validation

1. **Order Total Validation**: Ensure payment amount matches calculated order total
2. **Coupon Application**: Test payment flow with discount coupons
3. **Tax Calculation**: Verify tax amounts are correctly included in payment
4. **Tip Handling**: Test various tip scenarios in payment processing

### Compliance and Audit

1. **Payment Audit Trail**: Verify all payment attempts are properly logged
2. **PCI Compliance**: Ensure no sensitive payment data is logged or stored
3. **Refund Processing**: Test refund scenarios (future enhancement)

## Implementation Priority

### Phase 1: Core Payment Flow Tests

- Happy path online payment test
- Payment failure test
- COD payment test
- Basic webhook security test

### Phase 2: Edge Cases and Security

- Webhook idempotency tests
- Invalid signature tests
- Malformed payload tests
- Network failure scenarios

### Phase 3: Advanced Scenarios

- Concurrent payment tests
- Complex business logic validation
- Performance and load testing

## Alignment with Project Patterns

This testing strategy aligns with the existing YummyZoom project patterns:

- Uses existing `Testing` facade for consistent test infrastructure
- Follows established functional testing patterns from other features
- Leverages existing `CustomWebApplicationFactory` architecture
- Maintains separation of concerns between test layers
- Uses MediatR command pattern for test orchestration
- Follows DDD principles in test organization and structure

The implementation will provide comprehensive coverage of the payment integration while maintaining consistency with the project's architectural principles and testing standards.
