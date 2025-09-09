using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;

public sealed class UpdateTeamCartItemQuantityCommandHandler : IRequestHandler<UpdateTeamCartItemQuantityCommand, Result<Unit>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTeamCartItemQuantityCommandHandler> _logger;

    public UpdateTeamCartItemQuantityCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTeamCartItemQuantityCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Unit>> Handle(UpdateTeamCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);
            var itemId = TeamCartItemId.Create(request.TeamCartItemId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<Unit>(TeamCartErrors.TeamCartNotFound);
            }

            // Perform update via aggregate - domain handles all business rules (ownership, status, etc.)
            var updateResult = cart.UpdateItemQuantity(userId, itemId, request.NewQuantity);
            if (updateResult.IsFailure)
            {
                _logger.LogWarning("Failed to update item quantity. CartId={CartId} ItemId={ItemId} Error={Error}", request.TeamCartId, request.TeamCartItemId, updateResult.Error.Code);
                return Result.Failure<Unit>(updateResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Updated TeamCart item quantity. CartId={CartId} ItemId={ItemId} UserId={UserId} NewQty={Qty}", request.TeamCartId, request.TeamCartItemId, userId.Value, request.NewQuantity);

            // Redis VM update to be handled by domain event handler in a later phase.
            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}
