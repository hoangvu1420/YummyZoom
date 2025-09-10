using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate.Enums;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;

public sealed class ApplyCouponToTeamCartCommandHandler : IRequestHandler<ApplyCouponToTeamCartCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyCouponToTeamCartCommandHandler> _logger;
    private readonly OrderFinancialService _orderFinancialService;

    public ApplyCouponToTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        ICouponRepository couponRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        OrderFinancialService orderFinancialService,
        ILogger<ApplyCouponToTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _orderFinancialService = orderFinancialService ?? throw new ArgumentNullException(nameof(orderFinancialService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ApplyCouponToTeamCartCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Authorization handled by pipeline - user guaranteed to be authenticated and be TeamCart participant
            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure(TeamCartErrors.TeamCartNotFound);
            }

            // Resolve coupon by code within the same restaurant as the cart
            var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cart.RestaurantId, cancellationToken);
            if (coupon is null)
            {
                _logger.LogWarning("Coupon {CouponCode} not found for restaurant {RestaurantId}", request.CouponCode, cart.RestaurantId.Value);
                return Result.Failure(ApplyCouponToTeamCartErrors.CouponNotFound(request.CouponCode));
            }

            // Basic validity checks (do not increment usage here)
            var now = DateTime.UtcNow;
            if (!coupon.IsEnabled)
            {
                return Result.Failure(CouponErrors.CouponDisabled);
            }
            if (now < coupon.ValidityStartDate)
            {
                return Result.Failure(CouponErrors.CouponNotYetValid);
            }
            if (now > coupon.ValidityEndDate)
            {
                return Result.Failure(CouponErrors.CouponExpired);
            }

            // Enforce host-only and locked status before heavy validation
            if (userId != cart.HostUserId)
            {
                return Result.Failure(TeamCartErrors.OnlyHostCanModifyFinancials);
            }

            if (cart.Status != TeamCartStatus.Locked)
            {
                return Result.Failure(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
            }

            if (cart.AppliedCouponId is not null)
            {
                return Result.Failure(TeamCartErrors.CouponAlreadyApplied);
            }

            // Pre-validate applicability and compute discount against current cart items
            var discountCheck = _orderFinancialService.ValidateAndCalculateDiscountForTeamCartItems(
                coupon,
                cart.Items);
            if (discountCheck.IsFailure)
            {
                _logger.LogWarning("Coupon {CouponCode} not applicable for TeamCart {TeamCartId}: {Reason}", request.CouponCode, request.TeamCartId, discountCheck.Error.Code);
                return Result.Failure(discountCheck.Error);
            }

            var applyResult = cart.ApplyCoupon(userId, coupon.Id);
            if (applyResult.IsFailure)
            {
                _logger.LogWarning("Failed to apply coupon to TeamCart {TeamCartId}: {Reason}", request.TeamCartId, applyResult.Error.Code);
                return Result.Failure(applyResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Applied coupon to TeamCart. CartId={CartId} HostUserId={UserId} CouponCode={CouponCode}",
                request.TeamCartId, userId.Value, request.CouponCode);

            // VM update will be handled by dedicated event handler if/when we add CouponApplied events.
            return Result.Success();
        }, cancellationToken);
    }
}
