using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.PayoutAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Payouts.Commands.FailPayout;

public sealed record FailPayoutCommand(
    Guid PayoutId,
    string Reason
) : IRequest<Result>;

public sealed class FailPayoutCommandHandler : IRequestHandler<FailPayoutCommand, Result>
{
    private readonly IPayoutRepository _payoutRepository;
    private readonly IRestaurantAccountRepository _restaurantAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FailPayoutCommandHandler> _logger;

    public FailPayoutCommandHandler(
        IPayoutRepository payoutRepository,
        IRestaurantAccountRepository restaurantAccountRepository,
        IUnitOfWork unitOfWork,
        ILogger<FailPayoutCommandHandler> logger)
    {
        _payoutRepository = payoutRepository ?? throw new ArgumentNullException(nameof(payoutRepository));
        _restaurantAccountRepository = restaurantAccountRepository ?? throw new ArgumentNullException(nameof(restaurantAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> Handle(FailPayoutCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var payoutId = PayoutId.Create(request.PayoutId);
            var payout = await _payoutRepository.GetByIdAsync(payoutId, cancellationToken);
            if (payout is null)
            {
                return Result.Failure(FailPayoutErrors.NotFound);
            }

            if (payout.Status == PayoutStatus.Failed)
            {
                return Result.Success();
            }

            if (payout.Status == PayoutStatus.Completed)
            {
                return Result.Failure(FailPayoutErrors.AlreadyCompleted);
            }

            var account = await _restaurantAccountRepository.GetByRestaurantIdAsync(payout.RestaurantId, cancellationToken);
            if (account is null)
            {
                return Result.Failure(FailPayoutErrors.AccountNotFound);
            }

            var releaseResult = account.ReleasePayoutHold(payout.Amount);
            if (releaseResult.IsFailure)
            {
                return Result.Failure(releaseResult.Error);
            }

            var failResult = payout.MarkFailed(request.Reason);
            if (failResult.IsFailure)
            {
                return Result.Failure(failResult.Error);
            }

            await _restaurantAccountRepository.UpdateAsync(account, cancellationToken);
            await _payoutRepository.UpdateAsync(payout, cancellationToken);

            _logger.LogInformation("Payout {PayoutId} failed for restaurant {RestaurantId}", payout.Id.Value, payout.RestaurantId.Value);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class FailPayoutErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "FailPayout.NotFound", "Payout not found.");

    public static readonly Error AlreadyCompleted = Error.Conflict(
        "FailPayout.AlreadyCompleted", "Payout is already completed.");

    public static readonly Error AccountNotFound = Error.NotFound(
        "FailPayout.AccountNotFound", "Restaurant account not found.");
}
