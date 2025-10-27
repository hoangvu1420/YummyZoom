using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;

public sealed class CommitToCodPaymentCommandHandler : IRequestHandler<CommitToCodPaymentCommand, Result>
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

    public async Task<Result> Handle(CommitToCodPaymentCommand request, CancellationToken cancellationToken)
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

            // Validate quote version if provided
            if (request.QuoteVersion.HasValue && request.QuoteVersion != cart.QuoteVersion)
            {
                _logger.LogWarning("Quote version mismatch for TeamCart {TeamCartId}. Requested: {RequestedVersion}, Current: {CurrentVersion}", 
                    request.TeamCartId, request.QuoteVersion, cart.QuoteVersion);
                return Result.Failure(TeamCartErrors.QuoteVersionMismatch(cart.QuoteVersion));
            }

            // Use quoted per-member total when available (Quote Lite); fallback to items subtotal
            Money memberTotal;
            if (cart.QuoteVersion > 0)
            {
                var quoted = cart.GetMemberQuote(userId);
                if (quoted.IsFailure)
                {
                    _logger.LogWarning("No quote available for COD commit. TeamCartId={TeamCartId} UserId={UserId}", request.TeamCartId, userId.Value);
                    return Result.Failure(quoted.Error);
                }
                memberTotal = quoted.Value;
            }
            else
            {
                memberTotal = new Money(
                    cart.Items.Where(i => i.AddedByUserId == userId).Sum(i => i.LineItemTotal.Amount),
                    cart.TipAmount.Currency);
            }

            var commitResult = cart.CommitToCashOnDelivery(userId, memberTotal);
            if (commitResult.IsFailure)
            {
                _logger.LogWarning("Failed to commit COD payment for TeamCart {TeamCartId}: {Reason}", request.TeamCartId, commitResult.Error.Code);
                return Result.Failure(commitResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Member committed to COD payment. CartId={CartId} UserId={UserId} Amount={Amount}",
                request.TeamCartId, userId.Value, memberTotal.Amount);

            // VM update will be handled by outbox-driven event handlers
            return Result.Success();
        }, cancellationToken);
    }
}

