using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Result>
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IOrderRepository _orderRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(
        IPaymentGatewayService paymentGatewayService,
        IOrderRepository orderRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _paymentGatewayService = paymentGatewayService ?? throw new ArgumentNullException(nameof(paymentGatewayService));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1. Webhook Event Construction & Verification
            var webhookEventResult = _paymentGatewayService.ConstructWebhookEvent(
                request.RawJson,
                request.StripeSignatureHeader);

            if (webhookEventResult.IsFailure)
            {
                _logger.LogWarning("Webhook signature verification failed: {Error}", webhookEventResult.Error.Description);
                return Result.Failure(webhookEventResult.Error);
            }

            var webhookEvent = webhookEventResult.Value;
            var orderId = webhookEvent.Metadata?.TryGetValue("order_id", out var orderIdValue) == true
                ? orderIdValue
                : null;

            // If no OrderId, mark processed and exit successfully (not for us)
            if (string.IsNullOrWhiteSpace(orderId))
            {
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }

            _logger.LogInformation("Webhook event verified. EventId: {EventId}, EventType: {EventType}, RelevantObjectId: {RelevantObjectId}, OrderId: {OrderId}",
                webhookEvent.EventId, webhookEvent.EventType, webhookEvent.RelevantObjectId, orderId);

            // 2. Idempotency Check
            var existingEvent = await _dbContext.ProcessedWebhookEvents
                .FirstOrDefaultAsync(e => e.Id == webhookEvent.EventId, cancellationToken);

            if (existingEvent is not null)
            {
                _logger.LogInformation("Webhook event {EventId} already processed at {ProcessedAt}. Returning success.",
                    webhookEvent.EventId, existingEvent.ProcessedAt);
                return Result.Success();
            }

            // 3. Order Lookup (prefer metadata order_id, then fallback to PaymentIntent id)
            Domain.OrderAggregate.Order? order = null;
            if (!string.IsNullOrWhiteSpace(orderId) && Guid.TryParse(orderId, out var orderGuid))
            {
                order = await _orderRepository.GetByIdAsync(OrderId.Create(orderGuid), cancellationToken);
            }

            if (order is null)
            {
                order = await _orderRepository.GetByPaymentGatewayReferenceIdAsync(
                    webhookEvent.RelevantObjectId, cancellationToken);
            }

            if (order is null)
            {
                _logger.LogWarning("Order not found for payment gateway reference ID: {PaymentGatewayReferenceId}. Event might not be order-related.",
                    webhookEvent.RelevantObjectId);

                // Still mark as processed to avoid reprocessing
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }

            // 4. Event Processing
            var processingResult = webhookEvent.EventType switch
            {
                "payment_intent.succeeded" => ProcessPaymentSuccess(order, webhookEvent.RelevantObjectId),
                "payment_intent.payment_failed" => ProcessPaymentFailure(order, webhookEvent.RelevantObjectId),
                _ => ProcessUnsupportedEvent(webhookEvent.EventType)
            };

            if (processingResult.IsFailure)
            {
                _logger.LogError("Failed to process webhook event {EventId} of type {EventType}: {Error}",
                    webhookEvent.EventId, webhookEvent.EventType, processingResult.Error.Description);
                return processingResult;
            }

            // 5. Persistence
            if (webhookEvent.EventType is "payment_intent.succeeded" or "payment_intent.payment_failed")
            {
                await _orderRepository.UpdateAsync(order, cancellationToken);
            }

            await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }

    private Result ProcessPaymentSuccess(Domain.OrderAggregate.Order order, string paymentGatewayReferenceId)
    {
        var result = order.RecordPaymentSuccess(paymentGatewayReferenceId);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to record payment success for order {OrderId}: {Error}",
                order.Id.Value, result.Error.Description);
            return Result.Failure(HandleStripeWebhookErrors.EventProcessingFailed("payment_intent.succeeded"));
        }

        _logger.LogInformation("Payment success recorded for order {OrderId}", order.Id.Value);
        return Result.Success();
    }

    private Result ProcessPaymentFailure(Domain.OrderAggregate.Order order, string paymentGatewayReferenceId)
    {
        var result = order.RecordPaymentFailure(paymentGatewayReferenceId);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to record payment failure for order {OrderId}: {Error}",
                order.Id.Value, result.Error.Description);
            return Result.Failure(HandleStripeWebhookErrors.EventProcessingFailed("payment_intent.payment_failed"));
        }

        _logger.LogInformation("Payment failure recorded for order {OrderId}", order.Id.Value);
        return Result.Success();
    }

    private Result ProcessUnsupportedEvent(string eventType)
    {
        return Result.Success();
    }

    private async Task MarkEventAsProcessed(string eventId, CancellationToken cancellationToken)
    {
        var processedEvent = new ProcessedWebhookEvent
        {
            Id = eventId,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.ProcessedWebhookEvents.Add(processedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class HandleStripeWebhookErrors
{
    public static Error WebhookVerificationFailed() =>
        Error.Validation("HandleStripeWebhook.VerificationFailed", "Webhook signature verification failed.");

    public static Error EventProcessingFailed(string eventType) =>
        Error.Validation("HandleStripeWebhook.ProcessingFailed", $"Failed to process event type: {eventType}");
}
