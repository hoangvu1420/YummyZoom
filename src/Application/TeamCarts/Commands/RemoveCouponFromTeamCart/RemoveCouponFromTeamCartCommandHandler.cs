using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;

public sealed class RemoveCouponFromTeamCartCommandHandler : IRequestHandler<RemoveCouponFromTeamCartCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveCouponFromTeamCartCommandHandler> _logger;
    private readonly ITeamCartStore _teamCartStore;

    public RemoveCouponFromTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ITeamCartStore teamCartStore,
        ILogger<RemoveCouponFromTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _teamCartStore = teamCartStore ?? throw new ArgumentNullException(nameof(teamCartStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(RemoveCouponFromTeamCartCommand request, CancellationToken cancellationToken)
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

            // Perform coupon removal via aggregate - domain handles all business rules (host-only, locked state, etc.)
            var removeResult = cart.RemoveCoupon(userId);
            if (removeResult.IsFailure)
            {
                _logger.LogWarning("Failed to remove coupon from TeamCart {TeamCartId}: {Reason}", request.TeamCartId, removeResult.Error.Code);
                return Result.Failure(removeResult.Error);
            }

            // Recompute Quote Lite with zero discount
            var currency = cart.TipAmount.Currency;
            var memberSubtotals = cart.Items
                .GroupBy(i => i.AddedByUserId)
                .ToDictionary(g => g.Key, g => new Money(g.Sum(x => x.LineItemTotal.Amount), currency));

            var pricingConfig = StaticPricingService.GetPricingConfiguration(cart.RestaurantId);
            var feesTotal = pricingConfig.DeliveryFee;
            var tipAmount = cart.TipAmount;
            
            // Calculate tax based on policy using centralized service
            var cartSubtotal = new Money(memberSubtotals.Values.Sum(m => m.Amount), currency);
            var taxBase = StaticPricingService.CalculateTaxBase(cartSubtotal, feesTotal, tipAmount, pricingConfig.TaxBasePolicy);
            var taxAmount = new Money(taxBase.Amount * pricingConfig.TaxRate, currency);
            var discount = new Money(0m, currency);

            cart.ComputeQuoteLite(memberSubtotals, feesTotal, tipAmount, taxAmount, discount);

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Removed coupon from TeamCart. CartId={CartId} HostUserId={UserId}",
                request.TeamCartId, userId.Value);

            return Result.Success();
        }, cancellationToken);
    }
}
