using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text.Json;
using YummyZoom.Application.Common.Currency;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Infrastructure.Payments.Stripe;

public class StripeService : IPaymentGatewayService
{
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;
    private readonly string _webhookSecret;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeOptions> stripeOptions,
        ILogger<StripeService> logger)
    {
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
        _webhookSecret = stripeOptions.Value.WebhookSecret;
        _logger = logger;
    }

    public async Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(
        Money amount,
        string currency,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        metadata.TryGetValue("order_id", out var orderId);

        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = CurrencyMinorUnitConverter.ToMinorUnits(amount.Amount, currency),
                Currency = currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                },
                Metadata = new Dictionary<string, string>(metadata)
            };

            _logger.LogInformation(
                "Creating Stripe Payment Intent for Order ID: {OrderId}", orderId);

            var paymentIntent = await _paymentIntentService.CreateAsync(
                options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully created Stripe Payment Intent ID: {PaymentIntentId} for Order ID: {OrderId}",
                paymentIntent.Id, orderId);

            return Result.Success(
                new PaymentIntentResult(paymentIntent.Id, paymentIntent.ClientSecret));
        }
        catch (StripeException e)
        {
            _logger.LogError(
                e, "Stripe API error during payment intent creation for Order ID: {OrderId}", orderId);

            return Result.Failure<PaymentIntentResult>(
                Error.Failure("Stripe.CreatePaymentIntentFailed", e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, "Unexpected error during payment intent creation for Order ID: {OrderId}", orderId);

            return Result.Failure<PaymentIntentResult>(
                Error.Problem("Stripe.UnexpectedPaymentIntentError", e.Message));
        }
    }

    public Result<WebhookEventResult> ConstructWebhookEvent(string json, string stripeSignatureHeader)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, _webhookSecret);

            if (stripeEvent.Data.Object is not IHasId relevantObject)
            {
                _logger.LogWarning("Stripe webhook event data object does not contain an ID. Event ID: {EventId}", stripeEvent.Id);
                return Result.Failure<WebhookEventResult>(Error.Validation("Webhook.MissingId", "The webhook event data object does not contain an ID."));
            }

            // Attempt to get metadata from the object
            IDictionary<string, string>? metadata = null;
            if (stripeEvent.Data.Object is IHasMetadata objectWithMetadata)
            {
                metadata = objectWithMetadata.Metadata;
            }

            // Normalize RelevantObjectId: use PaymentIntent ID for charge.* events when available
            var relevantId = relevantObject.Id;
            if (!string.IsNullOrWhiteSpace(stripeEvent.Type) && stripeEvent.Type.StartsWith("charge."))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataEl)
                        && dataEl.TryGetProperty("object", out var objEl)
                        && objEl.TryGetProperty("payment_intent", out var piEl)
                        && piEl.ValueKind == JsonValueKind.String)
                    {
                        var pi = piEl.GetString();
                        if (!string.IsNullOrWhiteSpace(pi))
                        {
                            relevantId = pi!; // prefer PaymentIntent for correlation
                        }
                    }
                }
                catch (Exception)
                {
                    // Fall back silently to original relevantObject.Id
                }
            }

            var result = new WebhookEventResult(
                EventId: stripeEvent.Id,
                EventType: stripeEvent.Type,
                RelevantObjectId: relevantId,
                Metadata: metadata
            );
            
            return Result.Success(result);
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook signature validation failed.");
            return Result.Failure<WebhookEventResult>(Error.Validation("Stripe.WebhookSignatureInvalid", e.Message));
        }
    }

    public async Task<Result<string>> RefundPaymentAsync(string gatewayTransactionId, Money amountToRefund, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = gatewayTransactionId,
                Amount = CurrencyMinorUnitConverter.ToMinorUnits(amountToRefund.Amount, amountToRefund.Currency),
                Reason = reason
            };

            _logger.LogInformation("Creating Stripe Refund for Payment Intent ID: {PaymentIntentId}", gatewayTransactionId);
            var stripeRefund = await _refundService.CreateAsync(options, null, cancellationToken);

            if (stripeRefund.Status == "succeeded")
            {
                _logger.LogInformation("Successfully created Stripe Refund ID: {RefundId} for Payment Intent ID: {PaymentIntentId}", stripeRefund.Id, gatewayTransactionId);
                return Result.Success(stripeRefund.Id);
            }
            else
            {
                _logger.LogWarning("Stripe refund for Payment Intent ID: {PaymentIntentId} was not successful. Status: {Status}, Reason: {FailureReason}", gatewayTransactionId, stripeRefund.Status, stripeRefund.FailureReason);
                return Result.Failure<string>(Error.Failure("Stripe.RefundFailed", stripeRefund.FailureReason ?? "Refund was not successful."));
            }
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe API error during refund for transaction {TransactionId}", gatewayTransactionId);
            return Result.Failure<string>(Error.Failure("Stripe.RefundApiError", e.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during refund for transaction {TransactionId}", gatewayTransactionId);
            return Result.Failure<string>(Error.Problem("Stripe.UnexpectedRefundError", e.Message));
        }
    }
}
