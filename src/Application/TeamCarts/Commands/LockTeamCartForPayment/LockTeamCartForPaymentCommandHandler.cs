using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

public sealed class LockTeamCartForPaymentCommandHandler : IRequestHandler<LockTeamCartForPaymentCommand, Result<Unit>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LockTeamCartForPaymentCommandHandler> _logger;

    public LockTeamCartForPaymentCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<LockTeamCartForPaymentCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Unit>> Handle(LockTeamCartForPaymentCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<Unit>(TeamCartErrors.TeamCartNotFound);
            }

            var lockResult = cart.LockForPayment(userId);
            if (lockResult.IsFailure)
            {
                _logger.LogWarning("Failed to lock TeamCart {TeamCartId}: {Reason}", request.TeamCartId, lockResult.Error.Code);
                return Result.Failure<Unit>(lockResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("TeamCart locked for payment. CartId={CartId} HostUserId={UserId} Status={Status}",
                request.TeamCartId, userId.Value, cart.Status);

            // Redis VM update is handled by domain event handler for TeamCartLockedForPayment in a later step.
            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}

