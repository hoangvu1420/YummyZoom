using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed class FastCouponCheckQueryHandler : IRequestHandler<FastCouponCheckQuery, Result<CouponSuggestionsResponse>>
{
    private readonly IFastCouponCheckService _fastCouponCheckService;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICustomizationGroupRepository _customizationGroupRepository;
    private readonly IUser _currentUser;
    private readonly ILogger<FastCouponCheckQueryHandler> _logger;

    public FastCouponCheckQueryHandler(
        IFastCouponCheckService fastCouponCheckService,
        IMenuItemRepository menuItemRepository,
        ICustomizationGroupRepository customizationGroupRepository,
        IUser currentUser,
        ILogger<FastCouponCheckQueryHandler> logger)
    {
        _fastCouponCheckService = fastCouponCheckService ?? throw new ArgumentNullException(nameof(fastCouponCheckService));
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _customizationGroupRepository = customizationGroupRepository ?? throw new ArgumentNullException(nameof(customizationGroupRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CouponSuggestionsResponse>> Handle(FastCouponCheckQuery request, CancellationToken ct)
    {
        if (_currentUser.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        try
        {
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var userId = _currentUser.DomainUserId;

            // Load menu items by IDs (batch query)
            var menuItemIds = request.Items.Select(i => MenuItemId.Create(i.MenuItemId)).ToList();
            var menuItems = await _menuItemRepository.GetByIdsAsync(menuItemIds, ct);
            var menuItemDict = menuItems.ToDictionary(m => m.Id, m => m);

            // Convert request items to CartItem format with server-calculated prices
            var cartItems = new List<CartItem>();
            var skippedItems = 0;

            foreach (var requestItem in request.Items)
            {
                var menuItemId = MenuItemId.Create(requestItem.MenuItemId);
                
                if (!menuItemDict.TryGetValue(menuItemId, out var menuItem))
                {
                    _logger.LogWarning("Menu item {MenuItemId} not found, skipping from coupon check", requestItem.MenuItemId);
                    skippedItems++;
                    continue;
                }

                if (!menuItem.IsAvailable)
                {
                    _logger.LogWarning("Menu item {MenuItemId} is unavailable, skipping from coupon check", requestItem.MenuItemId);
                    skippedItems++;
                    continue;
                }

                // Build customizations
                var customizationsResult = await BuildOrderItemCustomizations(
                    menuItem, requestItem.Customizations, request.RestaurantId, ct);

                if (customizationsResult.IsFailure)
                {
                    _logger.LogWarning("Failed to build customizations for menu item {MenuItemId}: {Error}", 
                        requestItem.MenuItemId, customizationsResult.Error);
                    // Continue without customizations
                }

                // Create temporary OrderItem to calculate final price
                var orderItemResult = OrderItem.CreateTemporaryForPricing(
                    menuItem.MenuCategoryId,
                    menuItem.Id,
                    menuItem.Name,
                    menuItem.BasePrice.Copy(),
                    requestItem.Quantity,
                    customizationsResult.IsSuccess ? customizationsResult.Value : new List<OrderItemCustomization>());

                if (orderItemResult.IsFailure)
                {
                    _logger.LogWarning("Failed to create order item for menu item {MenuItemId}: {Error}", 
                        requestItem.MenuItemId, orderItemResult.Error);
                    skippedItems++;
                    continue;
                }

                var orderItem = orderItemResult.Value;
                
                // Calculate unit price: (basePrice + customization adjustments) per unit
                var unitPrice = orderItem.LineItemTotal.Amount / requestItem.Quantity;
                var currency = menuItem.BasePrice.Currency;

                // Convert to CartItem format for the service
                cartItems.Add(new CartItem(
                    menuItem.Id.Value,
                    menuItem.MenuCategoryId.Value,
                    requestItem.Quantity,
                    unitPrice,
                    currency));
            }

            if (!cartItems.Any())
            {
                _logger.LogWarning("No valid items found for coupon check for restaurant {RestaurantId}", request.RestaurantId);
                return Result.Failure<CouponSuggestionsResponse>(
                    Error.Validation("FastCouponCheck.NoValidItems", "No valid items found for coupon evaluation"));
            }

            if (skippedItems > 0)
            {
                _logger.LogInformation("Skipped {SkippedCount} invalid items during coupon check", skippedItems);
            }

            // Use the optimized fast coupon check service
            var suggestions = await _fastCouponCheckService.GetSuggestionsAsync(
                restaurantId, cartItems, userId, ct);

            _logger.LogInformation("Fast coupon check completed for restaurant {RestaurantId}, user {UserId}. " +
                                 "Cart subtotal: {Subtotal}, suggestions: {SuggestionCount}, best savings: {BestSavings}",
                restaurantId.Value, userId.Value, suggestions.CartSummary.Subtotal, 
                suggestions.Suggestions.Count, suggestions.BestDeal?.Savings ?? 0);

            return Result.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fast coupon check for restaurant {RestaurantId}", request.RestaurantId);
            return Result.Failure<CouponSuggestionsResponse>(Error.Failure("FastCouponCheck.ProcessingError", "An error occurred while processing coupon suggestions"));
        }
    }

    private async Task<Result<List<OrderItemCustomization>>> BuildOrderItemCustomizations(
        MenuItem menuItem,
        List<FastCouponCheckCustomizationDto>? requestCustomizations,
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (requestCustomizations is null || !requestCustomizations.Any())
        {
            return Result.Success(new List<OrderItemCustomization>());
        }

        var selectedCustomizations = new List<OrderItemCustomization>();

        // Build set of allowed customization group ids from menu item
        var allowedGroupIds = menuItem.AppliedCustomizations.Select(c => c.CustomizationGroupId).ToHashSet();

        // Collect all group ids for this item
        var itemGroupIds = requestCustomizations.Select(c => c.CustomizationGroupId).Distinct().ToList();

        // Load groups in batch
        var groupIdValueObjects = itemGroupIds.Select(id => CustomizationGroupId.Create(id)).ToList();
        var groups = await _customizationGroupRepository.GetByIdsAsync(groupIdValueObjects, cancellationToken);
        var groupDict = groups.ToDictionary(g => g.Id, g => g);

        foreach (var customizationReq in requestCustomizations)
        {
            var groupId = CustomizationGroupId.Create(customizationReq.CustomizationGroupId);
            
            if (!allowedGroupIds.Contains(groupId))
            {
                _logger.LogWarning("Customization group {GroupId} not applicable to menu item {MenuItemId}", 
                    customizationReq.CustomizationGroupId, menuItem.Id.Value);
                continue;
            }

            if (!groupDict.TryGetValue(groupId, out var group))
            {
                _logger.LogWarning("Customization group {GroupId} not found", customizationReq.CustomizationGroupId);
                continue;
            }

            // Validate and process choice IDs
            var choiceIdsDistinct = customizationReq.ChoiceIds.Distinct().ToList();
            if (choiceIdsDistinct.Count != customizationReq.ChoiceIds.Count)
            {
                _logger.LogWarning("Duplicate choice IDs found in customization group {GroupId}", customizationReq.CustomizationGroupId);
                continue;
            }

            var choiceDict = group.Choices.ToDictionary(c => c.Id, c => c);
            foreach (var choiceIdGuid in choiceIdsDistinct)
            {
                var choiceIdVo = ChoiceId.Create(choiceIdGuid);
                if (!choiceDict.TryGetValue(choiceIdVo, out var choice))
                {
                    _logger.LogWarning("Choice {ChoiceId} not found in group {GroupId}", choiceIdGuid, customizationReq.CustomizationGroupId);
                    continue;
                }

                // Build snapshot value object
                var customizationResult = OrderItemCustomization.Create(
                    group.GroupName,
                    choice.Name,
                    choice.PriceAdjustment.Copy());

                if (customizationResult.IsFailure)
                {
                    _logger.LogWarning("Failed to create customization snapshot for choice {ChoiceId} in group {GroupId}: {Error}", 
                        choiceIdGuid, customizationReq.CustomizationGroupId, customizationResult.Error);
                    continue;
                }

                selectedCustomizations.Add(customizationResult.Value);
            }
        }

        return Result.Success(selectedCustomizations);
    }
}
