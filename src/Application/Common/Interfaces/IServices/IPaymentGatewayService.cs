using Stripe;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices
{
    public interface IPaymentGatewayService
    {
        Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(Money amount, string currency, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);
        
        Result<WebhookEventResult> ConstructWebhookEvent(string json, string stripeSignatureHeader);
        
        Task<Result<string>> RefundPaymentAsync(string gatewayTransactionId, Money amountToRefund, string reason, CancellationToken cancellationToken = default);
    }
}
