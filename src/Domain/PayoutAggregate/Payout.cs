using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.PayoutAggregate.Errors;
using YummyZoom.Domain.PayoutAggregate.Events;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.PayoutAggregate;

public sealed class Payout : AggregateRoot<PayoutId, Guid>, ICreationAuditable
{
    public RestaurantAccountId RestaurantAccountId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public Money Amount { get; private set; }
    public PayoutStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? ProviderReferenceId { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; }

    // Creation audit properties (immutable aggregate)
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    private Payout(
        PayoutId id,
        RestaurantAccountId restaurantAccountId,
        RestaurantId restaurantId,
        Money amount,
        string idempotencyKey,
        PayoutStatus status,
        DateTimeOffset requestedAt)
        : base(id)
    {
        RestaurantAccountId = restaurantAccountId;
        RestaurantId = restaurantId;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
        Status = status;
        RequestedAt = requestedAt;
        Created = requestedAt;
    }

    public static Result<Payout> Create(
        RestaurantAccountId restaurantAccountId,
        RestaurantId restaurantId,
        Money amount,
        string idempotencyKey,
        DateTimeOffset? requestedAt = null)
    {
        if (amount.Amount <= 0)
        {
            return Result.Failure<Payout>(PayoutErrors.AmountMustBePositive);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<Payout>(PayoutErrors.IdempotencyKeyRequired);
        }

        var createdAt = requestedAt ?? DateTimeOffset.UtcNow;
        var payout = new Payout(
            PayoutId.CreateUnique(),
            restaurantAccountId,
            restaurantId,
            amount,
            idempotencyKey.Trim(),
            PayoutStatus.Requested,
            createdAt);

        payout.AddDomainEvent(new PayoutRequested(
            payout.Id,
            payout.RestaurantAccountId,
            payout.Amount));

        return Result.Success(payout);
    }

    public Result MarkProcessing(string? providerReferenceId = null)
    {
        if (Status != PayoutStatus.Requested)
        {
            return Result.Failure(PayoutErrors.InvalidStatusTransition(Status, PayoutStatus.Processing));
        }

        Status = PayoutStatus.Processing;
        if (!string.IsNullOrWhiteSpace(providerReferenceId))
        {
            ProviderReferenceId = providerReferenceId.Trim();
        }

        AddDomainEvent(new PayoutProcessing(Id, ProviderReferenceId));
        return Result.Success();
    }

    public Result MarkCompleted()
    {
        if (Status != PayoutStatus.Processing)
        {
            return Result.Failure(PayoutErrors.InvalidStatusTransition(Status, PayoutStatus.Completed));
        }

        Status = PayoutStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PayoutCompleted(Id));
        return Result.Success();
    }

    public Result MarkFailed(string reason)
    {
        if (Status != PayoutStatus.Requested && Status != PayoutStatus.Processing)
        {
            return Result.Failure(PayoutErrors.InvalidStatusTransition(Status, PayoutStatus.Failed));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(PayoutErrors.FailureReasonRequired);
        }

        Status = PayoutStatus.Failed;
        FailureReason = reason.Trim();
        FailedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PayoutFailed(Id, FailureReason));
        return Result.Success();
    }

#pragma warning disable CS8618
    private Payout() { }
#pragma warning restore CS8618
}
