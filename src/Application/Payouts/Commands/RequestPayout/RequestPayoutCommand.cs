using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.PayoutAggregate.Errors;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Payouts.Commands.RequestPayout;

[Authorize(Policy = Policies.MustBeRestaurantOwner)]
public sealed record RequestPayoutCommand(
    Guid RestaurantGuid,
    decimal? Amount,
    string? IdempotencyKey = null
) : IRequest<Result<RequestPayoutResponse>>, IRestaurantCommand, IIdempotentCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => RestaurantId.Create(RestaurantGuid);
}

public sealed record RequestPayoutResponse(
    Guid PayoutId,
    string Status,
    decimal Amount,
    string Currency);

public sealed class RequestPayoutCommandHandler : IRequestHandler<RequestPayoutCommand, Result<RequestPayoutResponse>>
{
    private static readonly TimeSpan WeeklyCadence = TimeSpan.FromDays(7);

    private readonly IRestaurantAccountRepository _restaurantAccountRepository;
    private readonly IPayoutRepository _payoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<RequestPayoutCommandHandler> _logger;

    public RequestPayoutCommandHandler(
        IRestaurantAccountRepository restaurantAccountRepository,
        IPayoutRepository payoutRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<RequestPayoutCommandHandler> logger)
    {
        _restaurantAccountRepository = restaurantAccountRepository ?? throw new ArgumentNullException(nameof(restaurantAccountRepository));
        _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<RequestPayoutResponse>> Handle(RequestPayoutCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (_currentUser.DomainUserId is null)
            {
            _logger.LogWarning("Unauthenticated user attempting to request payout for restaurant {RestaurantId}", request.RestaurantGuid);
            throw new UnauthorizedAccessException();
        }

            var restaurantId = RestaurantId.Create(request.RestaurantGuid);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _payoutRepository.GetByIdempotencyKeyAsync(
                    restaurantId,
                    request.IdempotencyKey.Trim(),
                    cancellationToken);

                if (existing is not null)
                {
                    _logger.LogInformation("Payout request idempotency hit for restaurant {RestaurantId}", request.RestaurantGuid);
                    return Result.Success(new RequestPayoutResponse(
                        existing.Id.Value,
                        existing.Status.ToString(),
                        existing.Amount.Amount,
                        existing.Amount.Currency));
                }
            }

            var account = await _restaurantAccountRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
            if (account is null)
            {
                return Result.Failure<RequestPayoutResponse>(RequestPayoutErrors.AccountNotFound);
            }

            if (account.PayoutMethodDetails is null)
            {
                return Result.Failure<RequestPayoutResponse>(RequestPayoutErrors.PayoutMethodMissing);
            }

            var cadenceAnchor = await _payoutRepository.GetLatestCompletedAtAsync(restaurantId, cancellationToken)
                ?? await _payoutRepository.GetLatestRequestedAtAsync(restaurantId, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (cadenceAnchor.HasValue && cadenceAnchor.Value.Add(WeeklyCadence) > now)
            {
                return Result.Failure<RequestPayoutResponse>(RequestPayoutErrors.WeeklyCadenceViolation(cadenceAnchor.Value.Add(WeeklyCadence)));
            }

            var availableBalance = account.GetAvailableBalance();
            if (availableBalance.Amount <= 0)
            {
                return Result.Failure<RequestPayoutResponse>(RequestPayoutErrors.InsufficientAvailableBalance);
            }

            var payoutAmountValue = request.Amount ?? availableBalance.Amount;
            var payoutAmount = new Money(payoutAmountValue, availableBalance.Currency);
            if (payoutAmount.Amount <= 0)
            {
                return Result.Failure<RequestPayoutResponse>(PayoutErrors.AmountMustBePositive);
            }

            var reserveResult = account.ReservePayout(payoutAmount);
            if (reserveResult.IsFailure)
            {
                return Result.Failure<RequestPayoutResponse>(reserveResult.Error);
            }

            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? Guid.NewGuid().ToString("N")
                : request.IdempotencyKey.Trim();

            var payoutResult = Payout.Create(account.Id, restaurantId, payoutAmount, idempotencyKey, now);
            if (payoutResult.IsFailure)
            {
                return Result.Failure<RequestPayoutResponse>(payoutResult.Error);
            }

            await _payoutRepository.AddAsync(payoutResult.Value, cancellationToken);
            await _restaurantAccountRepository.UpdateAsync(account, cancellationToken);

            _logger.LogInformation(
                "Payout requested for restaurant {RestaurantId} amount {Amount} {Currency}",
                request.RestaurantGuid,
                payoutAmount.Amount,
                payoutAmount.Currency);

            return Result.Success(new RequestPayoutResponse(
                payoutResult.Value.Id.Value,
                PayoutStatus.Requested.ToString(),
                payoutAmount.Amount,
                payoutAmount.Currency));
        }, cancellationToken);
    }
}

public static class RequestPayoutErrors
{
    public static readonly Error AccountNotFound = Error.NotFound(
        "RequestPayout.AccountNotFound", "Restaurant account not found.");

    public static readonly Error PayoutMethodMissing = Error.Validation(
        "RequestPayout.PayoutMethodMissing", "Payout method is required before requesting a payout.");

    public static readonly Error InsufficientAvailableBalance = Error.Validation(
        "RequestPayout.InsufficientAvailableBalance", "Available balance is insufficient for payout.");

    public static Error WeeklyCadenceViolation(DateTimeOffset nextEligibleAt) => Error.Validation(
        "RequestPayout.WeeklyCadenceViolation", $"Next eligible payout time is {nextEligibleAt:O}.");
}
