using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;

public sealed class CommitToCodPaymentCommandHandler : IRequestHandler<CommitToCodPaymentCommand, Result<Unit>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CommitToCodPaymentCommandHandler> _logger;

    public CommitToCodPaymentCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<CommitToCodPaymentCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Unit>> Handle(CommitToCodPaymentCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (_currentUser.DomainUserId is null)
            {
                throw new UnauthorizedAccessException();
            }

            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<Unit>(TeamCartErrors.TeamCartNotFound);
            }

            // Compute the member's total using the cart's currency
            var memberTotal = new Money(
                cart.Items.Where(i => i.AddedByUserId == userId).Sum(i => i.LineItemTotal.Amount),
                cart.TipAmount.Currency);

            var commitResult = cart.CommitToCashOnDelivery(userId, memberTotal);
            if (commitResult.IsFailure)
            {
                _logger.LogWarning("Failed to commit COD payment for TeamCart {TeamCartId}: {Reason}", request.TeamCartId, commitResult.Error.Code);
                return Result.Failure<Unit>(commitResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Member committed to COD payment. CartId={CartId} UserId={UserId} Amount={Amount}",
                request.TeamCartId, userId.Value, memberTotal.Amount);

            // VM update will be handled by outbox-driven event handlers
            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}


