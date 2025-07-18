using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Entities;

/// <summary>
/// Represents a payment commitment or transaction by a team cart member.
/// This entity tracks both payment commitments (COD) and completed online payments.
/// </summary>
public sealed class MemberPayment : Entity<MemberPaymentId>
{
    /// <summary>
    /// Gets the ID of the user who made this payment.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the amount of money for this payment.
    /// </summary>
    public Money Amount { get; private set; }

    /// <summary>
    /// Gets the payment method (Online or CashOnDelivery).
    /// </summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>
    /// Gets the current status of this payment.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Gets the online transaction ID if this was an online payment.
    /// </summary>
    public string? OnlineTransactionId { get; private set; }

    /// <summary>
    /// Gets the timestamp when this payment was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when this payment was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPayment"/> class.
    /// Private constructor enforced by DDD for controlled creation via static factory method.
    /// </summary>
    private MemberPayment(
        MemberPaymentId id,
        UserId userId,
        Money amount,
        PaymentMethod method,
        DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        Amount = amount;
        Method = method;
        Status = method == PaymentMethod.CashOnDelivery ? PaymentStatus.CommittedToCOD : PaymentStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private MemberPayment() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new member payment instance.
    /// </summary>
    /// <param name="userId">The ID of the user making the payment.</param>
    /// <param name="amount">The amount of the payment.</param>
    /// <param name="method">The payment method.</param>
    /// <returns>A <see cref="Result{MemberPayment}"/> indicating success or failure.</returns>
    public static Result<MemberPayment> Create(
        UserId userId,
        Money amount,
        PaymentMethod method)
    {
        // Validate amount
        if (amount.Amount <= 0)
        {
            return Result.Failure<MemberPayment>(TeamCartErrors.InvalidPaymentAmount);
        }

        // Validate payment method
        if (!Enum.IsDefined(typeof(PaymentMethod), method))
        {
            return Result.Failure<MemberPayment>(TeamCartErrors.InvalidPaymentMethod);
        }

        var payment = new MemberPayment(
            MemberPaymentId.CreateUnique(),
            userId,
            amount,
            method,
            DateTime.UtcNow);

        return Result.Success(payment);
    }

    /// <summary>
    /// Marks the payment as successfully paid online with a transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID from the payment processor.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsPaidOnline(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return Result.Failure(TeamCartErrors.MemberPaymentInvalidTransactionId);
        }

        if (Method != PaymentMethod.Online)
        {
            return Result.Failure(TeamCartErrors.NotOnlinePayment);
        }

        if (Status == PaymentStatus.PaidOnline)
        {
            return Result.Success(); // Already paid, idempotent operation
        }

        Status = PaymentStatus.PaidOnline;
        OnlineTransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Marks the online payment as failed.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsFailed()
    {
        if (Method != PaymentMethod.Online)
        {
            return Result.Failure(TeamCartErrors.NotOnlinePayment);
        }

        if (Status == PaymentStatus.Failed)
        {
            return Result.Success(); // Already failed, idempotent operation
        }

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Checks if this payment is complete (either paid online or committed to COD).
    /// </summary>
    /// <returns>True if the payment is complete, false otherwise.</returns>
    public bool IsComplete()
    {
        return Status == PaymentStatus.PaidOnline || Status == PaymentStatus.CommittedToCOD;
    }

    /// <summary>
    /// Checks if this payment has failed.
    /// </summary>
    /// <returns>True if the payment has failed, false otherwise.</returns>
    public bool HasFailed()
    {
        return Status == PaymentStatus.Failed;
    }

    /// <summary>
    /// Gets a display name for the payment status.
    /// </summary>
    /// <returns>A human-readable description of the payment status.</returns>
    public string GetStatusDisplayName()
    {
        return Status switch
        {
            PaymentStatus.Pending => "Pending Payment",
            PaymentStatus.CommittedToCOD => "Cash on Delivery",
            PaymentStatus.PaidOnline => "Paid Online",
            PaymentStatus.Failed => "Payment Failed",
            _ => "Unknown Status"
        };
    }
}
