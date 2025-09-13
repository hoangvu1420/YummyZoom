using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.CouponAggregate.Events;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Commands.InitiateOrder;

public class InitiateOrderCommandHandler : IRequestHandler<InitiateOrderCommand, Result<InitiateOrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ICustomizationGroupRepository _customizationGroupRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly OrderFinancialService _orderFinancialService;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<InitiateOrderCommandHandler> _logger;

    public InitiateOrderCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
    ICouponRepository couponRepository,
    ICustomizationGroupRepository customizationGroupRepository,
        IPaymentGatewayService paymentGatewayService,
        OrderFinancialService orderFinancialService,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        IMediator mediator,
        ILogger<InitiateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _restaurantRepository = restaurantRepository ?? throw new ArgumentNullException(nameof(restaurantRepository));
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
    _customizationGroupRepository = customizationGroupRepository ?? throw new ArgumentNullException(nameof(customizationGroupRepository));
        _paymentGatewayService = paymentGatewayService ?? throw new ArgumentNullException(nameof(paymentGatewayService));
        _orderFinancialService = orderFinancialService ?? throw new ArgumentNullException(nameof(orderFinancialService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<InitiateOrderResponse>> Handle(InitiateOrderCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Convert simple types to domain value objects
            var customerId = UserId.Create(request.CustomerId);
            var restaurantId = RestaurantId.Create(request.RestaurantId);

            // Convert string to PaymentMethodType enum
            if (!Enum.TryParse<PaymentMethodType>(request.PaymentMethod, true, out var paymentMethodType))
            {
                _logger.LogWarning("Invalid payment method: {PaymentMethod}", request.PaymentMethod);
                return Result.Failure<InitiateOrderResponse>(Error.Validation("InitiateOrder.InvalidPaymentMethod", "The specified payment method is not valid."));
            }

            // Validate customer is the same as current user (Authorization check)
            if (_currentUser.DomainUserId is null)
            {
                _logger.LogWarning("User is not authenticated");
                throw new UnauthorizedAccessException();
            }

            if (!_currentUser.DomainUserId.Equals(customerId))
            {
                _logger.LogWarning("User {CurrentUserId} attempting to create order for different customer {CustomerId}",
                    _currentUser.DomainUserId.Value, customerId.Value);
                throw new ForbiddenAccessException();
            }

            // 1. Validate restaurant exists and is active
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant {RestaurantId} not found", restaurantId.Value);
                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.RestaurantNotFound());
            }

            if (!restaurant.IsActive())
            {
                _logger.LogWarning("Restaurant {RestaurantId} is not active", restaurantId.Value);
                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.RestaurantNotActive());
            }

            // 2. Validate menu items exist and are available
            var menuItemIds = request.Items.Select(i => MenuItemId.Create(i.MenuItemId)).ToList();

            // Get unique menu item IDs to avoid duplicate database lookups
            var uniqueMenuItemIds = menuItemIds.Distinct().ToList();

            var menuItems = await _menuItemRepository.GetByIdsAsync(uniqueMenuItemIds, cancellationToken);

            // Check if all unique menu items were found
            if (menuItems.Count != uniqueMenuItemIds.Count)
            {
                var foundIds = menuItems.Select(m => m.Id).ToHashSet();
                var missingIds = uniqueMenuItemIds.Where(id => !foundIds.Contains(id)).ToList();
                _logger.LogWarning("Menu items not found: {MissingIds}", string.Join(", ", missingIds.Select(id => id.Value)));
                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.MenuItemsNotFound());
            }

            string currency = menuItems.First().GetCurrency();

            // Validate all menu items belong to the restaurant
            var invalidItems = menuItems.Where(m => m.RestaurantId != restaurantId).ToList();
            if (invalidItems.Count != 0)
            {
                _logger.LogWarning("Menu items {InvalidItemIds} do not belong to restaurant {RestaurantId}",
                    string.Join(", ", invalidItems.Select(i => i.Id.Value)), restaurantId.Value);
                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.MenuItemsNotFromRestaurant());
            }

            // Check availability
            foreach (var menuItem in menuItems.Where(menuItem => !menuItem.IsAvailable))
            {
                _logger.LogWarning("Menu item {MenuItemId} is not available", menuItem.Id.Value);
                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.MenuItemNotAvailable(menuItem.Name));
            }

            // 3. Create delivery address value object
            var deliveryAddressResult = DeliveryAddress.Create(
                request.DeliveryAddress.Street,
                request.DeliveryAddress.City,
                request.DeliveryAddress.State,
                request.DeliveryAddress.ZipCode,
                request.DeliveryAddress.Country);

            if (deliveryAddressResult.IsFailure)
            {
                _logger.LogWarning("Invalid delivery address: {Error}", deliveryAddressResult.Error);
                return Result.Failure<InitiateOrderResponse>(deliveryAddressResult.Error);
            }

            // 4. Create order items with current pricing and selected customizations
            var orderItems = new List<OrderItem>();
            foreach (var requestItem in request.Items)
            {
                var menuItemId = MenuItemId.Create(requestItem.MenuItemId);
                var menuItem = menuItems.First(m => m.Id == menuItemId);
                var selectedCustomizations = new List<OrderItemCustomization>();

                if (requestItem.Customizations is not null && requestItem.Customizations.Count > 0)
                {
                    // Build set of allowed customization group ids from menu item
                    var allowedGroupIds = menuItem.AppliedCustomizations.Select(c => c.CustomizationGroupId).ToHashSet();

                    // Collect all group ids for this item
                    var itemGroupIds = requestItem.Customizations.Select(c => c.CustomizationGroupId).Distinct().ToList();

                    // Load groups in batch
                    var groupIdValueObjects = itemGroupIds.Select(id => CustomizationGroupId.Create(id)).ToList();
                    var groups = await _customizationGroupRepository.GetByIdsAsync(groupIdValueObjects, cancellationToken);
                    var groupDict = groups.ToDictionary(g => g.Id, g => g);

                    foreach (var customizationReq in requestItem.Customizations)
                    {
                        var groupIdVo = CustomizationGroupId.Create(customizationReq.CustomizationGroupId);

                        if (!allowedGroupIds.Contains(groupIdVo))
                        {
                            _logger.LogWarning("Customization group {GroupId} not assigned to menu item {MenuItemId}", customizationReq.CustomizationGroupId, menuItem.Id.Value);
                            return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.CustomizationGroupNotAssignedToMenuItem(customizationReq.CustomizationGroupId));
                        }

                        if (!groupDict.TryGetValue(groupIdVo, out var group) || group.IsDeleted || group.RestaurantId != restaurantId)
                        {
                            _logger.LogWarning("Customization group {GroupId} not found or invalid for restaurant {RestaurantId}", customizationReq.CustomizationGroupId, restaurantId.Value);
                            return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.CustomizationGroupNotFound(customizationReq.CustomizationGroupId));
                        }

                        // Validate selection count
                        var choiceIdsDistinct = customizationReq.ChoiceIds.Distinct().ToList();
                        if (choiceIdsDistinct.Count < group.MinSelections || choiceIdsDistinct.Count > group.MaxSelections)
                        {
                            _logger.LogWarning("Selection count {Count} invalid for group {GroupId} (min {Min} max {Max})", choiceIdsDistinct.Count, customizationReq.CustomizationGroupId, group.MinSelections, group.MaxSelections);
                            return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.CustomizationGroupSelectionCountInvalid(customizationReq.CustomizationGroupId, group.MinSelections, group.MaxSelections));
                        }

                        var choiceDict = group.Choices.ToDictionary(c => c.Id, c => c);
                        foreach (var choiceIdGuid in choiceIdsDistinct)
                        {
                            var choiceIdVo = ChoiceId.Create(choiceIdGuid);
                            if (!choiceDict.TryGetValue(choiceIdVo, out var choice))
                            {
                                _logger.LogWarning("Choice {ChoiceId} not found in group {GroupId}", choiceIdGuid, customizationReq.CustomizationGroupId);
                                return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.CustomizationChoiceNotFound(choiceIdGuid));
                            }

                            // Build snapshot value object
                            var customizationResult = OrderItemCustomization.Create(
                                group.GroupName,
                                choice.Name,
                                choice.PriceAdjustment.Copy());

                            if (customizationResult.IsFailure)
                            {
                                _logger.LogWarning("Failed to create customization snapshot for choice {ChoiceId} in group {GroupId}: {Error}", choiceIdGuid, customizationReq.CustomizationGroupId, customizationResult.Error);
                                return Result.Failure<InitiateOrderResponse>(customizationResult.Error);
                            }

                            selectedCustomizations.Add(customizationResult.Value);
                        }
                    }
                }

                var orderItemResult = OrderItem.Create(
                    menuItem.MenuCategoryId,
                    menuItem.Id,
                    menuItem.Name,
                    menuItem.BasePrice.Copy(),
                    requestItem.Quantity,
                    selectedCustomizations.Any() ? selectedCustomizations : null);

                if (orderItemResult.IsFailure)
                {
                    _logger.LogWarning("Failed to create order item for {MenuItemId}: {Error}",
                        menuItem.Id.Value, orderItemResult.Error);
                    return Result.Failure<InitiateOrderResponse>(orderItemResult.Error);
                }

                orderItems.Add(orderItemResult.Value);
            }

            // 5. Calculate financial amounts using OrderFinancialService
            var subtotal = _orderFinancialService.CalculateSubtotal(orderItems);

            var discountAmount = new Money(0, currency);
            CouponId? appliedCouponId = null;
            if (!string.IsNullOrEmpty(request.CouponCode))
            {
                var normalizedCouponCode = request.CouponCode.Trim().ToUpperInvariant();
                var coupon = await _couponRepository.GetByCodeAsync(normalizedCouponCode, restaurantId, cancellationToken);
                
                if (coupon is not null)
                {
                    // 1) Validate & compute discount (no usage checks here)
                    var validationResult = _orderFinancialService.ValidateAndCalculateDiscount(
                        coupon, orderItems, subtotal);

                    if (validationResult.IsFailure)
                    {
                        _logger.LogWarning("Coupon pre-validation failed for {CouponCode}: {Error}", request.CouponCode, validationResult.Error);
                        return Result.Failure<InitiateOrderResponse>(validationResult.Error);
                    }

                    // 2-3) Finalize usage (per-user + total) and enqueue outbox event in the same transaction
                    var finalizeOk = await _couponRepository.FinalizeUsageAsync(
                        coupon.Id,
                        customerId,
                        coupon.UsageLimitPerUser,
                        cancellationToken);

                    if (!finalizeOk)
                    {
                        _logger.LogWarning("Failed to finalize coupon usage for {CouponCode}; limits exhausted or concurrency conflict.", request.CouponCode);
                        return Result.Failure<InitiateOrderResponse>(CouponErrors.UsageLimitExceeded);
                    }

                    discountAmount = validationResult.Value;
                    appliedCouponId = coupon.Id;
                    _logger.LogInformation("Successfully applied and incremented coupon {CouponCode} with discount {DiscountAmount}",
                        request.CouponCode, discountAmount.Amount);
                }
                else
                {
                    _logger.LogWarning("Coupon {CouponCode} not found for restaurant {RestaurantId}",
                        request.CouponCode, restaurantId.Value);
                    return Result.Failure<InitiateOrderResponse>(InitiateOrderErrors.CouponNotFound(request.CouponCode));
                }
            }

            var tipAmount = new Money(request.TipAmount ?? 0m, currency);
            var deliveryFee = new Money(2.99m, currency); 
            var taxAmount = new Money(subtotal.Amount * 0.08m, currency); 

            var totalAmount = _orderFinancialService.CalculateFinalTotal(
                subtotal, discountAmount, deliveryFee, tipAmount, taxAmount);

            // 6. Generate OrderId before payment intent creation for metadata
            var orderId = OrderId.CreateUnique();

            // 7. Handle payment processing for online payments first
            string? paymentIntentId = null;
            string? clientSecret = null;

            if (paymentMethodType != PaymentMethodType.CashOnDelivery)
            {
                // Create payment intent for online payments with order_id in metadata
                var metadata = new Dictionary<string, string>
                {
                    ["source"] = "order",
                    ["user_id"] = customerId.Value.ToString(),
                    ["restaurant_id"] = restaurantId.Value.ToString(),
                    ["order_id"] = orderId.Value.ToString()
                };

                var paymentResult = await _paymentGatewayService.CreatePaymentIntentAsync(
                    totalAmount,
                    currency,
                    metadata,
                    cancellationToken);

                if (paymentResult.IsFailure)
                {
                    _logger.LogError("Failed to create payment intent: {Error}", paymentResult.Error);
                    return Result.Failure<InitiateOrderResponse>(paymentResult.Error);
                }

                paymentIntentId = paymentResult.Value.PaymentIntentId;
                clientSecret = paymentResult.Value.ClientSecret;

                _logger.LogInformation("Created payment intent {PaymentIntentId}", paymentIntentId);
            }

            // 8. Create the order with payment gateway reference if needed
            var orderResult = Order.Create(
                orderId,
                customerId,
                restaurantId,
                deliveryAddressResult.Value,
                orderItems,
                request.SpecialInstructions ?? string.Empty,
                subtotal,
                discountAmount,
                deliveryFee,
                tipAmount,
                taxAmount,
                totalAmount,
                paymentMethodType,
                appliedCouponId,
                paymentIntentId,
                request.TeamCartId.HasValue ? TeamCartId.Create(request.TeamCartId.Value) : null);

            if (orderResult.IsFailure)
            {
                _logger.LogWarning("Failed to create order: {Error}", orderResult.Error);
                return Result.Failure<InitiateOrderResponse>(orderResult.Error);
            }

            var order = orderResult.Value;

            if (paymentMethodType == PaymentMethodType.CashOnDelivery)
            {
                _logger.LogInformation("Order {OrderId} created with Cash on Delivery payment method", order.Id.Value);
            }

            // 8. Save the order
            await _orderRepository.AddAsync(order, cancellationToken);

            _logger.LogInformation("Successfully created order {OrderId} for user {UserId} at restaurant {RestaurantId}",
                order.Id.Value, customerId.Value, restaurantId.Value);

            return Result.Success(new InitiateOrderResponse(
                order.Id,
                order.OrderNumber,
                order.TotalAmount,
                paymentIntentId,
                clientSecret));
        }, cancellationToken);
    }
}

// Application-specific error definitions for InitiateOrder command
public static class InitiateOrderErrors
{
    public static Error RestaurantNotFound() =>
        Error.NotFound("InitiateOrder.RestaurantNotFound", "The specified restaurant was not found.");

    public static Error RestaurantNotActive() =>
        Error.Validation("InitiateOrder.RestaurantNotActive", "The restaurant is currently not accepting orders.");

    public static Error MenuItemsNotFound() =>
        Error.NotFound("InitiateOrder.MenuItemsNotFound", "One or more menu items were not found.");

    public static Error MenuItemsNotFromRestaurant() =>
        Error.Validation("InitiateOrder.MenuItemsNotFromRestaurant", "All menu items must belong to the same restaurant.");

    public static Error MenuItemNotAvailable(string itemName) =>
        Error.Validation("InitiateOrder.MenuItemNotAvailable", $"Menu item '{itemName}' is currently not available.");

    public static Error CouponNotFound(string couponCode) =>
        Error.NotFound("Coupon.CouponNotFound", $"The specified coupon code {couponCode} is not valid.");

    // Customization related
    public static Error CustomizationGroupNotFound(Guid groupId) =>
        Error.NotFound("InitiateOrder.CustomizationGroupNotFound", $"Customization group {groupId} was not found.");

    public static Error CustomizationGroupNotAssignedToMenuItem(Guid groupId) =>
        Error.Validation("InitiateOrder.CustomizationGroupNotAssignedToMenuItem", $"Customization group {groupId} is not assigned to the selected menu item.");

    public static Error CustomizationGroupSelectionCountInvalid(Guid groupId, int min, int max) =>
        Error.Validation("InitiateOrder.CustomizationGroupSelectionCountInvalid", $"Customization group {groupId} requires between {min} and {max} selections.");

    public static Error CustomizationChoiceNotFound(Guid choiceId) =>
        Error.NotFound("InitiateOrder.CustomizationChoiceNotFound", $"Customization choice {choiceId} was not found in the specified group.");
}
