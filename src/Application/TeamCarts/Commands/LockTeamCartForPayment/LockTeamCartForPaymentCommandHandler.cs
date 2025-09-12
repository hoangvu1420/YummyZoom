using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;

public sealed class LockTeamCartForPaymentCommandHandler : IRequestHandler<LockTeamCartForPaymentCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LockTeamCartForPaymentCommandHandler> _logger;
    private readonly ITeamCartStore _teamCartStore;
    private readonly ICouponRepository _couponRepository;
    private readonly OrderFinancialService _financialService;

    public LockTeamCartForPaymentCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ICouponRepository couponRepository,
        OrderFinancialService financialService,
        ITeamCartStore teamCartStore,
        ILogger<LockTeamCartForPaymentCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
        _financialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
        _teamCartStore = teamCartStore ?? throw new ArgumentNullException(nameof(teamCartStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(LockTeamCartForPaymentCommand request, CancellationToken cancellationToken)
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

            var lockResult = cart.LockForPayment(userId);
            if (lockResult.IsFailure)
            {
                _logger.LogWarning("Failed to lock TeamCart {TeamCartId}: {Reason}", request.TeamCartId, lockResult.Error.Code);
                return Result.Failure(lockResult.Error);
            }

            // Compute Quote Lite after locking
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
            await _teamCartStore.UpdateQuoteAsync(cart.Id, cart.QuoteVersion,
                cart.MemberTotals.ToDictionary(k => k.Key.Value, v => v.Value.Amount), currency, cancellationToken);

            _logger.LogInformation("TeamCart locked for payment. CartId={CartId} HostUserId={UserId} Status={Status}",
                request.TeamCartId, userId.Value, cart.Status);

            // Redis VM update is handled by domain event handler for TeamCartLockedForPayment in a later step.
            return Result.Success();
        }, cancellationToken);
    }
}
