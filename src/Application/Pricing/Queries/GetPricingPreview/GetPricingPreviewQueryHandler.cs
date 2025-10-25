using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Pricing.Queries.GetPricingPreview;

public class GetPricingPreviewQueryHandler : IRequestHandler<GetPricingPreviewQuery, Result<GetPricingPreviewResponse>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICustomizationGroupRepository _customizationGroupRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly OrderFinancialService _orderFinancialService;
    private readonly ILogger<GetPricingPreviewQueryHandler> _logger;

    public GetPricingPreviewQueryHandler(
        IMenuItemRepository menuItemRepository,
        ICustomizationGroupRepository customizationGroupRepository,
        ICouponRepository couponRepository,
        IRestaurantRepository restaurantRepository,
        OrderFinancialService orderFinancialService,
        ILogger<GetPricingPreviewQueryHandler> logger)
    {
        _menuItemRepository = menuItemRepository;
        _customizationGroupRepository = customizationGroupRepository;
        _couponRepository = couponRepository;
        _restaurantRepository = restaurantRepository;
        _orderFinancialService = orderFinancialService;
        _logger = logger;
    }

    public async Task<Result<GetPricingPreviewResponse>> Handle(
        GetPricingPreviewQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Validate restaurant exists and is active
        var restaurantId = RestaurantId.Create(request.RestaurantId);
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
        
        if (restaurant is null || !restaurant.IsActive())
        {
            _logger.LogWarning("Restaurant {RestaurantId} not found or inactive", request.RestaurantId);
            return Result.Failure<GetPricingPreviewResponse>(
                PricingPreviewErrors.RestaurantNotFoundOrInactive);
        }

        // 2. Load and validate menu items
        var menuItemIds = request.Items.Select(i => MenuItemId.Create(i.MenuItemId)).ToList();
        var menuItems = await _menuItemRepository.GetByIdsAsync(menuItemIds, cancellationToken);
        
        var notes = new List<PricingPreviewNoteDto>();
        var validOrderItems = new List<OrderItem>();

        // 3. Build temporary OrderItems for pricing calculation
        foreach (var requestItem in request.Items)
        {
            var menuItemId = MenuItemId.Create(requestItem.MenuItemId);
            var menuItem = menuItems.FirstOrDefault(m => m.Id == menuItemId);
            
            if (menuItem is null)
            {
                notes.Add(new PricingPreviewNoteDto("error", "MENU_ITEM_NOT_FOUND", 
                    $"Menu item {requestItem.MenuItemId} not found"));
                continue;
            }

            if (!menuItem.IsAvailable)
            {
                notes.Add(new PricingPreviewNoteDto("warning", "MENU_ITEM_UNAVAILABLE", 
                    $"Menu item '{menuItem.Name}' is currently unavailable"));
                continue;
            }

            // Build customizations using existing patterns from InitiateOrderCommandHandler
            var customizations = await BuildOrderItemCustomizations(
                menuItem, requestItem.Customizations, request.RestaurantId, cancellationToken);
            
            if (customizations.IsFailure)
            {
                notes.Add(new PricingPreviewNoteDto("error", "CUSTOMIZATION_INVALID", 
                    customizations.Error.Description));
                continue;
            }

            // Check if customizations were requested but none were successfully processed
            if (requestItem.Customizations is not null && requestItem.Customizations.Any() && !customizations.Value.Any())
            {
                notes.Add(new PricingPreviewNoteDto("error", "CUSTOMIZATION_INVALID", 
                    "One or more customizations could not be applied"));
                // Continue processing the item without customizations instead of skipping it
            }

            // Create temporary OrderItem for pricing calculation
            var orderItemResult = OrderItem.CreateTemporaryForPricing(
                menuItem.MenuCategoryId,
                menuItem.Id,
                menuItem.Name,
                menuItem.BasePrice.Copy(),
                requestItem.Quantity,
                customizations.Value);
            
            if (orderItemResult.IsFailure)
            {
                notes.Add(new PricingPreviewNoteDto("error", "ORDER_ITEM_CREATION_FAILED", 
                    orderItemResult.Error.Description));
                continue;
            }

            validOrderItems.Add(orderItemResult.Value);
        }

        if (!validOrderItems.Any())
        {
            _logger.LogWarning("No valid items found for pricing calculation for restaurant {RestaurantId}", request.RestaurantId);
            return Result.Failure<GetPricingPreviewResponse>(
                PricingPreviewErrors.NoValidItems);
        }

        // 4. Calculate subtotal
        var subtotal = _orderFinancialService.CalculateSubtotal(validOrderItems);
        
        // 5. Apply coupon if provided
        Money? discountAmount = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(
                request.CouponCode.Trim().ToUpperInvariant(), 
                restaurantId, 
                cancellationToken);
            
            if (coupon is not null)
            {
                var discountResult = _orderFinancialService.ValidateAndCalculateDiscount(
                    coupon, validOrderItems, subtotal);
                
                if (discountResult.IsSuccess)
                {
                    discountAmount = discountResult.Value;
                    notes.Add(new PricingPreviewNoteDto("info", "COUPON_APPLIED", 
                        $"Coupon '{request.CouponCode}' applied successfully"));
                }
                else
                {
                    notes.Add(new PricingPreviewNoteDto("warning", "COUPON_INVALID", 
                        $"Coupon '{request.CouponCode}': {discountResult.Error.Description}"));
                }
            }
            else
            {
                notes.Add(new PricingPreviewNoteDto("warning", "COUPON_NOT_FOUND", 
                    $"Coupon '{request.CouponCode}' not found"));
            }
        }

        // 6. Get pricing configuration using centralized static service (MVP approach)
        var pricingConfig = StaticPricingService.GetPricingConfiguration(restaurantId);
        
        var tipAmount = new Money(request.TipAmount ?? 0m, subtotal.Currency);
        var deliveryFee = pricingConfig.DeliveryFee;
        
        // Calculate tax based on policy using centralized service
        var taxBase = StaticPricingService.CalculateTaxBase(subtotal, deliveryFee, tipAmount, pricingConfig.TaxBasePolicy);
        var taxAmount = new Money(taxBase.Amount * pricingConfig.TaxRate, subtotal.Currency);

        // 7. Calculate final total using enhanced financial service with static pricing
        var finalTotal = _orderFinancialService.CalculateFinalTotalWithStaticPricing(
            restaurantId,
            subtotal, 
            discountAmount ?? Money.Zero(subtotal.Currency), 
            tipAmount);

        return Result.Success(new GetPricingPreviewResponse(
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            finalTotal,
            subtotal.Currency,
            notes,
            DateTime.UtcNow
        ));
    }

    private async Task<Result<List<OrderItemCustomization>>> BuildOrderItemCustomizations(
        MenuItem menuItem,
        List<PricingPreviewCustomizationDto>? requestCustomizations,
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
                // For pricing preview, we continue with empty customizations but the error will be handled by the caller
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
