namespace YummyZoom.Application.Common.Models;

// This record standardizes the result from creating a payment intent.
public record PaymentIntentResult(string PaymentIntentId, string ClientSecret);

// This record standardizes the result from processing a refund.
public record RefundResult(
    bool IsSuccess,
    string GatewayRefundId,
    string? FailureReason = null);
