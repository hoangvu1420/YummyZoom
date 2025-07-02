using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.Entities;

public sealed class PaymentTransaction : Entity<PaymentTransactionId>
{
    public PaymentMethodType PaymentMethodType { get; private set; }
    public string? PaymentMethodDisplay { get; private set; }
    public PaymentTransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? PaymentGatewayReferenceId { get; private set; }

    private PaymentTransaction(
        PaymentTransactionId paymentTransactionId,
        PaymentMethodType paymentMethodType,
        string? paymentMethodDisplay,
        PaymentTransactionType type,
        Money amount,
        DateTime timestamp,
        string? paymentGatewayReferenceId)
        : base(paymentTransactionId)
    {
        PaymentMethodType = paymentMethodType;
        PaymentMethodDisplay = paymentMethodDisplay;
        Type = type;
        Amount = amount;
        Status = PaymentStatus.Pending;
        Timestamp = timestamp;
        PaymentGatewayReferenceId = paymentGatewayReferenceId;
    }

    public static Result<PaymentTransaction> Create(
        PaymentMethodType paymentMethodType,
        PaymentTransactionType type,
        Money amount,
        DateTime timestamp,
        string? paymentMethodDisplay = null,
        string? paymentGatewayReferenceId = null)
    {
        if (amount.Amount <= 0)
        {
            return Result.Failure<PaymentTransaction>(OrderErrors.PaymentTransactionInvalidAmount);
        }

        return new PaymentTransaction(
            PaymentTransactionId.CreateUnique(),
            paymentMethodType,
            paymentMethodDisplay,
            type,
            amount,
            timestamp,
            paymentGatewayReferenceId);
    }

    public void MarkAsSucceeded()
    {
        Status = PaymentStatus.Succeeded;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
    }

#pragma warning disable CS8618
    private PaymentTransaction() { }
#pragma warning restore CS8618
}
