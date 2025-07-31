# HandleStripeWebhookCommand Implementation Analysis & Outline

## Project Analysis

### Current Architecture Overview

The YummyZoom project follows Clean Architecture with DDD principles, organized into layers:

- **Domain Layer**: Core business logic, entities, aggregates, domain events
- **Application Layer**: CQRS pattern with Commands/Queries, handlers, DTOs, validators
- **Infrastructure Layer**: Concrete implementations, data access, external services
- **Web Layer**: API endpoints and presentation logic

### Application Layer Patterns

Based on the Application Layer Guidelines and existing `InitiateOrderCommand` implementation:

#### Command Pattern Structure

- Commands use `IRequest<Result<TResponse>>` from MediatR
- Handlers implement `IRequestHandler<TCommand, Result<TResponse>>`
- All commands are wrapped in `IUnitOfWork.ExecuteInTransactionAsync`
- Authorization via `[Authorize]` attributes
- Validation using FluentValidation with `AbstractValidator<T>`
- Consistent error handling using `Result` pattern

#### Existing Infrastructure

- **IPaymentGatewayService**: Already has `ConstructWebhookEvent(string json, string stripeSignatureHeader)` method
- **ProcessedWebhookEvent**: Entity exists in `Application.Common.Models` with `Id` (string) and `ProcessedAt` (DateTime)
- **ApplicationDbContext**: Has `DbSet<ProcessedWebhookEvent>` configured
- **IOrderRepository**: Has `GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId)` method
- **Order Aggregate**: Has `RecordPaymentSuccess(string paymentGatewayReferenceId)` and `RecordPaymentFailure(string paymentGatewayReferenceId)` methods

## Implementation Outline

### 1. Command Structure

#### File: `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommand.cs`

```csharp
public record HandleStripeWebhookCommand(
    string RawJson,
    string StripeSignatureHeader
) : IRequest<Result>;
```

**Key Design Decisions:**

- No authorization required (webhook endpoint is public)
- Returns `Result` (not `Result<T>`) since webhooks only need success/failure indication
- Takes raw JSON and signature header as required by Stripe webhook verification

### 2. Command Handler Structure

#### File: `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs`

**Dependencies Required:**

- `IPaymentGatewayService` - for webhook event construction and verification
- `IOrderRepository` - for finding orders by payment gateway reference ID
- `IApplicationDbContext` - for direct access to ProcessedWebhookEvents (no repository pattern needed)
- `IUnitOfWork` - for transaction management
- `ILogger<HandleStripeWebhookCommandHandler>` - for logging

**Handler Logic Flow:**

1. **Webhook Event Construction & Verification**
   - Call `_paymentGatewayService.ConstructWebhookEvent(request.RawJson, request.StripeSignatureHeader)`
   - If verification fails, return failure result immediately
   - Extract `WebhookEventResult` with `EventId`, `EventType`, and `RelevantObjectId`

2. **Idempotency Check**
   - Check if `stripeEvent.EventId` exists in `ProcessedWebhookEvents` table
   - If exists, return success immediately (already processed)
   - Use raw SQL or direct DbContext access for performance

3. **Order Lookup**
   - Use `_orderRepository.GetByPaymentGatewayReferenceIdAsync(webhookEventResult.RelevantObjectId)`
   - If order not found, log warning and return success (event might not be order-related)

4. **Event Processing**
   - Switch on `webhookEventResult.EventType`:
     - `"payment_intent.succeeded"`: Call `order.RecordPaymentSuccess(webhookEventResult.RelevantObjectId)`
     - `"payment_intent.payment_failed"`: Call `order.RecordPaymentFailure(webhookEventResult.RelevantObjectId)`
     - Other events: Log info and return success (not handled)

5. **Persistence**
   - Add processed event record to `ProcessedWebhookEvents`
   - Update order via `_orderRepository.UpdateAsync(order)`
   - All wrapped in transaction via `IUnitOfWork.ExecuteInTransactionAsync`

### 3. Error Handling Strategy

#### Application-Specific Errors

Define custom errors for webhook processing in the same file as the command handler.

```csharp
public static class HandleStripeWebhookErrors
{
    public static Error WebhookVerificationFailed() => 
        Error.Validation("HandleStripeWebhook.VerificationFailed", "Webhook signature verification failed.");
    
    public static Error EventProcessingFailed(string eventType) => 
        Error.Validation("HandleStripeWebhook.ProcessingFailed", $"Failed to process event type: {eventType}");
}
```

### 4. Validation Considerations

#### File: `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandValidator.cs`

**Validation Rules:**

- `RawJson`: NotEmpty, valid JSON format
- `StripeSignatureHeader`: NotEmpty, proper Stripe signature format

### 5. Integration Points

#### Database Access Pattern

- **ProcessedWebhookEvents**: Direct `IApplicationDbContext` access (no repository needed for simple CRUD)
- **Orders**: Use existing `IOrderRepository` following established patterns
- **Transaction Management**: Use `IUnitOfWork.ExecuteInTransactionAsync` for consistency

#### Logging Strategy

- Log webhook event reception and verification
- Log idempotency checks (duplicate events)
- Log order lookup results
- Log successful/failed payment processing
- Use structured logging with event IDs and order IDs

### 6. Performance Considerations

- **Idempotency Check**: Use efficient database query on indexed `Id` field
- **Order Lookup**: Leverage existing repository method with proper indexing
- **Transaction Scope**: Keep transaction minimal to avoid long-running locks
- **Error Handling**: Fail fast on verification errors

### 7. Security Considerations

- **Webhook Verification**: Always verify Stripe signature before processing
- **Idempotency**: Prevent replay attacks and duplicate processing
- **Error Information**: Don't leak sensitive information in error responses
- **Logging**: Avoid logging sensitive payment information

## Next Steps

1. Implement `HandleStripeWebhookCommand` record
2. Implement `HandleStripeWebhookCommandHandler` with full logic
3. Implement `HandleStripeWebhookCommandValidator`
4. Add comprehensive unit tests
5. Add integration tests for webhook processing scenarios
6. Update Web layer endpoint to use the new command

## Alignment with Existing Patterns

This implementation follows the established patterns in the codebase:

- Consistent with `InitiateOrderCommand` structure and error handling
- Uses same dependency injection and transaction patterns
- Follows Application Layer Guidelines for command implementation
- Maintains separation of concerns between layers
- Uses existing domain methods for payment state transitions
- Aligns with the design specified in `/Docs/Feature-Discover/10-Order.md` for webhook processing
- Uses payment gateway reference ID for order lookup (PaymentIntent.Id from Stripe webhook)
