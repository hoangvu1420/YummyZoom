using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Payouts.Commands.CompletePayout;

public sealed record CompletePayoutCommand(
    Guid PayoutId,
    string? ProviderReferenceId = null
) : IRequest<Result>;

public sealed class CompletePayoutCommandHandler : IRequestHandler<CompletePayoutCommand, Result>
{
    private readonly IPayoutRepository _payoutRepository;
    private readonly IRestaurantAccountRepository _restaurantAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompletePayoutCommandHandler> _logger;

    public CompletePayoutCommandHandler(
        IPayoutRepository payoutRepository,
        IRestaurantAccountRepository restaurantAccountRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompletePayoutCommandHandler> logger)
    {
        _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        _restaurantAccountRepository = restaurantAccountRepository ?? throw new ArgumentNullException(nameof(restaurantAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> Handle(CompletePayoutCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var payoutId = PayoutId.Create(request.PayoutId);
            var payout = await _payoutRepository.GetByIdAsync(payoutId, cancellationToken);
            if (payout is null)
            {
                return Result.Failure(CompletePayoutErrors.NotFound);
            }

            if (payout.Status == PayoutStatus.Completed)
            {
                return Result.Success();
            }

            if (payout.Status == PayoutStatus.Failed)
            {
                return Result.Failure(CompletePayoutErrors.AlreadyFailed);
            }

            if (payout.Status == PayoutStatus.Requested && !string.IsNullOrWhiteSpace(request.ProviderReferenceId))
            {
                var processingResult = payout.MarkProcessing(request.ProviderReferenceId);
                if (processingResult.IsFailure)
                {
                    return Result.Failure(processingResult.Error);
                }
            }

            var completeResult = payout.MarkCompleted();
            if (completeResult.IsFailure)
            {
                return Result.Failure(completeResult.Error);
            }

            var account = await _restaurantAccountRepository.GetByRestaurantIdAsync(payout.RestaurantId, cancellationToken);
            if (account is null)
            {
                return Result.Failure(CompletePayoutErrors.AccountNotFound);
            }

            var releaseResult = account.ReleasePayoutHold(payout.Amount);
            if (releaseResult.IsFailure)
            {
                return Result.Failure(releaseResult.Error);
            }

            var settleResult = account.SettlePayout(payout.Amount);
            if (settleResult.IsFailure)
            {
                return Result.Failure(settleResult.Error);
            }

            await _restaurantAccountRepository.UpdateAsync(account, cancellationToken);
            await _payoutRepository.UpdateAsync(payout, cancellationToken);

            _logger.LogInformation("Payout {PayoutId} completed for restaurant {RestaurantId}", payout.Id.Value, payout.RestaurantId.Value);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class CompletePayoutErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "CompletePayout.NotFound", "Payout not found.");

    public static readonly Error AlreadyFailed = Error.Conflict(
        "CompletePayout.AlreadyFailed", "Payout is already marked as failed.");

    public static readonly Error AccountNotFound = Error.NotFound(
        "CompletePayout.AccountNotFound", "Restaurant account not found.");
}
