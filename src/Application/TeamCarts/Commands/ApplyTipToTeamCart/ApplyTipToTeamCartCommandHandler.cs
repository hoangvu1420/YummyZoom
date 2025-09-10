using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;

public sealed class ApplyTipToTeamCartCommandHandler : IRequestHandler<ApplyTipToTeamCartCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyTipToTeamCartCommandHandler> _logger;

    public ApplyTipToTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<ApplyTipToTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ApplyTipToTeamCartCommand request, CancellationToken cancellationToken)
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

            // Use the cart's existing currency for consistency
            var tipMoney = new Money(request.TipAmount, cart.TipAmount.Currency);
            var applyResult = cart.ApplyTip(userId, tipMoney);
            if (applyResult.IsFailure)
            {
                _logger.LogWarning("Failed to apply tip to TeamCart {TeamCartId}: {Reason}", request.TeamCartId, applyResult.Error.Code);
                return Result.Failure(applyResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Applied tip to TeamCart. CartId={CartId} HostUserId={UserId} Tip={Tip}",
                request.TeamCartId, userId.Value, request.TipAmount);

            // VM update will be handled by a domain event handler if/when we raise a TipApplied event.
            return Result.Success();
        }, cancellationToken);
    }
}

