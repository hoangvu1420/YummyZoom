using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;

public sealed class ConvertTeamCartToOrderCommandHandler : IRequestHandler<ConvertTeamCartToOrderCommand, Result<ConvertTeamCartToOrderResponse>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly TeamCartConversionService _conversionService;
    private readonly OrderFinancialService _financialService;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConvertTeamCartToOrderCommandHandler> _logger;

    public ConvertTeamCartToOrderCommandHandler(
        ITeamCartRepository teamCartRepository,
        IRestaurantRepository restaurantRepository,
        ICouponRepository couponRepository,
        IOrderRepository orderRepository,
        TeamCartConversionService conversionService,
        OrderFinancialService financialService,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<ConvertTeamCartToOrderCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _restaurantRepository = restaurantRepository ?? throw new ArgumentNullException(nameof(restaurantRepository));
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
        _financialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ConvertTeamCartToOrderResponse>> Handle(ConvertTeamCartToOrderCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (_currentUser.DomainUserId is null)
            {
                throw new UnauthorizedAccessException();
            }

            var teamCartId = TeamCartId.Create(request.TeamCartId);
            var cart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
            if (cart is null)
            {
                return Result.Failure<ConvertTeamCartToOrderResponse>(TeamCartErrors.TeamCartNotFound);
            }

            if (!_currentUser.DomainUserId.Equals(cart.HostUserId))
            {
                return Result.Failure<ConvertTeamCartToOrderResponse>(TeamCartErrors.OnlyHostCanModifyFinancials);
            }

            // Must be ReadyToConfirm
            if (cart.Status != Domain.TeamCartAggregate.Enums.TeamCartStatus.ReadyToConfirm)
            {
                return Result.Failure<ConvertTeamCartToOrderResponse>(TeamCartErrors.InvalidStatusForConversion);
            }

            // Build delivery address VO
            var addressResult = DeliveryAddress.Create(
                request.Street, request.City, request.State, request.ZipCode, request.Country);
            if (addressResult.IsFailure)
            {
                return Result.Failure<ConvertTeamCartToOrderResponse>(addressResult.Error);
            }

            // Load coupon entity if applied
            Domain.CouponAggregate.Coupon? coupon = null;
            if (cart.AppliedCouponId is not null)
            {
                coupon = await _couponRepository.GetByIdAsync(cart.AppliedCouponId, cancellationToken);
            }

            // Compute delivery fee and tax (placeholder logic; refine per restaurant policy)
            var currency = cart.TipAmount.Currency;
            var deliveryFee = new Money(2.99m, currency);
            var taxAmount = new Money(0m, currency); // can be computed via tax service in future

            // Compute and validate discount if coupon exists
            var discount = new Money(0m, currency);
            if (coupon is not null)
            {
                var discountResult = _financialService.ValidateAndCalculateDiscountForTeamCartItems(
                    coupon,
                    cart.Items);
                if (discountResult.IsFailure)
                {
                    return Result.Failure<ConvertTeamCartToOrderResponse>(discountResult.Error);
                }
                discount = discountResult.Value;

                // Atomically enforce usage limits prior to conversion
                var perUserIncOk = await _couponRepository.TryIncrementUserUsageCountAsync(
                    coupon.Id,
                    cart.HostUserId,
                    coupon.UsageLimitPerUser,
                    cancellationToken);
                if (!perUserIncOk)
                {
                    return Result.Failure<ConvertTeamCartToOrderResponse>(Domain.CouponAggregate.Errors.CouponErrors.UserUsageLimitExceeded);
                }

                var totalIncOk = await _couponRepository.TryIncrementUsageCountAsync(coupon.Id, cancellationToken);
                if (!totalIncOk)
                {
                    return Result.Failure<ConvertTeamCartToOrderResponse>(Domain.CouponAggregate.Errors.CouponErrors.UsageLimitExceeded);
                }
            }

            var conversion = _conversionService.ConvertToOrder(
                cart,
                addressResult.Value,
                request.SpecialInstructions ?? string.Empty,
                coupon,
                discount,
                deliveryFee,
                taxAmount);

            if (conversion.IsFailure)
            {
                return Result.Failure<ConvertTeamCartToOrderResponse>(conversion.Error);
            }

            var (order, updatedCart) = conversion.Value;

            // Persist updated cart and new order
            await _teamCartRepository.UpdateAsync(updatedCart, cancellationToken);
            await _orderRepository.AddAsync(order, cancellationToken);

            return Result.Success(new ConvertTeamCartToOrderResponse(order.Id.Value));
        }, cancellationToken);
    }
}
