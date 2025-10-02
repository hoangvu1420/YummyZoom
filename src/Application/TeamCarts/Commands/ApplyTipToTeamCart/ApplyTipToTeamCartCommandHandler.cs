using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Services;
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
    private readonly ITeamCartStore _teamCartStore;
    private readonly ICouponRepository _couponRepository;
    private readonly OrderFinancialService _financialService;

    public ApplyTipToTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ICouponRepository couponRepository,
        OrderFinancialService financialService,
        ITeamCartStore teamCartStore,
        ILogger<ApplyTipToTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
        _financialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
        _teamCartStore = teamCartStore ?? throw new ArgumentNullException(nameof(teamCartStore));
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

            // Recompute Quote Lite
            var currency = cart.TipAmount.Currency;
            var memberSubtotals = cart.Items
                .GroupBy(i => i.AddedByUserId)
                .ToDictionary(g => g.Key, g => new Money(g.Sum(x => x.LineItemTotal.Amount), currency));

            var feesTotal = new Money(2.99m, currency); // MVP placeholder
            var tipAmount = cart.TipAmount;
            var taxAmount = new Money(0m, currency); // MVP placeholder

            var discount = new Money(0m, currency);
            if (cart.AppliedCouponId is not null)
            {
                var coupon = await _couponRepository.GetByIdAsync(cart.AppliedCouponId, cancellationToken);
                if (coupon is not null)
                {
                    var d = _financialService.ValidateAndCalculateDiscountForTeamCartItems(coupon, cart.Items);
                    if (d.IsSuccess)
                    {
                        discount = d.Value;
                    }
                }
            }

            cart.ComputeQuoteLite(memberSubtotals, feesTotal, tipAmount, taxAmount, discount);

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Applied tip to TeamCart. CartId={CartId} HostUserId={UserId} Tip={Tip}",
                request.TeamCartId, userId.Value, request.TipAmount);

            // VM quote update will be handled by TeamCartQuoteUpdated outbox event handler.
            return Result.Success();
        }, cancellationToken);
    }
}
