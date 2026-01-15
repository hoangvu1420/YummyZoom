using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Currency;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;

public sealed class InitiateMemberOnlinePaymentCommandHandler : IRequestHandler<InitiateMemberOnlinePaymentCommand, Result<InitiateMemberOnlinePaymentResponse>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InitiateMemberOnlinePaymentCommandHandler> _logger;

    public InitiateMemberOnlinePaymentCommandHandler(
        ITeamCartRepository teamCartRepository,
        IPaymentGatewayService paymentGatewayService,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<InitiateMemberOnlinePaymentCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _paymentGatewayService = paymentGatewayService ?? throw new ArgumentNullException(nameof(paymentGatewayService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<InitiateMemberOnlinePaymentResponse>> Handle(InitiateMemberOnlinePaymentCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(TeamCartErrors.TeamCartNotFound);
            }

            // Validate payments can be initiated only when cart is finalized
            if (cart.Status != TeamCartStatus.Finalized)
            {
                _logger.LogWarning("Cannot initiate online payment when cart is not finalized. CartId={TeamCartId} Status={Status}", request.TeamCartId, cart.Status);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(TeamCartErrors.CanOnlyPayOnFinalizedCart);
            }

            // Validate user is a member
            if (!cart.Members.Any(m => m.UserId == userId))
            {
                _logger.LogWarning("User {UserId} is not a member of TeamCart {TeamCartId}", userId.Value, request.TeamCartId);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(TeamCartErrors.UserNotMember);
            }

            // Validate quote version if provided
            if (request.QuoteVersion.HasValue && request.QuoteVersion != cart.QuoteVersion)
            {
                _logger.LogWarning("Quote version mismatch for TeamCart {TeamCartId}. Requested: {RequestedVersion}, Current: {CurrentVersion}", 
                    request.TeamCartId, request.QuoteVersion, cart.QuoteVersion);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(TeamCartErrors.QuoteVersionMismatch(cart.QuoteVersion));
            }

            // Use quoted per-member total (Quote Lite)
            var quoted = cart.GetMemberQuote(userId);
            if (quoted.IsFailure)
            {
                _logger.LogWarning("No quote available for user {UserId} on TeamCart {TeamCartId}", userId.Value, request.TeamCartId);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(quoted.Error);
            }
            var memberTotal = quoted.Value;

            // Create payment intent with teamcart metadata; do not mutate aggregate
            var metadata = new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = cart.Id.Value.ToString(),
                ["member_user_id"] = userId.Value.ToString(),
                ["quote_version"] = cart.QuoteVersion.ToString(),
                // Stripe amount is expressed in minor units; keep webhook cross-check in the same unit.
                ["quoted_cents"] = CurrencyMinorUnitConverter.ToMinorUnits(memberTotal.Amount, memberTotal.Currency).ToString()
            };

            var intentResult = await _paymentGatewayService.CreatePaymentIntentAsync(
                memberTotal,
                memberTotal.Currency,
                metadata,
                cancellationToken);

            if (intentResult.IsFailure)
            {
                _logger.LogError("Failed to create payment intent for team cart {TeamCartId}: {Error}", request.TeamCartId, intentResult.Error);
                return Result.Failure<InitiateMemberOnlinePaymentResponse>(intentResult.Error);
            }

            _logger.LogInformation("Created member payment intent {PaymentIntentId} for team cart {TeamCartId}", intentResult.Value.PaymentIntentId, request.TeamCartId);

            return Result.Success(new InitiateMemberOnlinePaymentResponse(
                intentResult.Value.PaymentIntentId,
                intentResult.Value.ClientSecret));
        }, cancellationToken);
    }
}
