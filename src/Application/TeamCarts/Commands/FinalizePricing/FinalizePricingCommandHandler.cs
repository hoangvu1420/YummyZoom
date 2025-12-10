using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.FinalizePricing;

public sealed class FinalizePricingCommandHandler : IRequestHandler<FinalizePricingCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FinalizePricingCommandHandler> _logger;

    public FinalizePricingCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<FinalizePricingCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(FinalizePricingCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure(TeamCartErrors.TeamCartNotFound);
            }

            var finalizeResult = cart.FinalizePricing(userId);
            if (finalizeResult.IsFailure)
            {
                _logger.LogWarning("Failed to finalize pricing for TeamCart {TeamCartId}: {Reason}", 
                    request.TeamCartId, finalizeResult.Error.Code);
                return Result.Failure(finalizeResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Pricing finalized for TeamCart. CartId={CartId} HostUserId={UserId} Status={Status}",
                request.TeamCartId, userId.Value, cart.Status);

            // VM status update will be handled by TeamCartPricingFinalized outbox event handler
            return Result.Success();
        }, cancellationToken);
    }
}
