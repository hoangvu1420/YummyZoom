using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Options;
using YummyZoom.SharedKernel;
using OrderAggregate = YummyZoom.Domain.OrderAggregate.Order;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.OrderSeeders;

/// <summary>
/// Seeder for creating realistic order data using direct domain services.
/// Generates orders with proper status distribution, realistic timestamps, and valid business scenarios.
/// Uses the domain model directly to ensure all business logic and domain events are preserved.
/// </summary>
public class OrderSeeder : ISeeder
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly OrderFinancialService _orderFinancialService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OrderSeeder> _logger;

    public OrderSeeder(
        IOrderRepository orderRepository,
        IUserAggregateRepository userRepository,
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        ICouponRepository couponRepository,
        OrderFinancialService orderFinancialService,
        ApplicationDbContext dbContext,
        ILogger<OrderSeeder> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _couponRepository = couponRepository;
        _orderFinancialService = orderFinancialService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public string Name => "Order";
    public int Order => 120; // After Coupons (115)

    public async Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Check if we have the required dependencies
        var hasUsers = await _dbContext.DomainUsers.AnyAsync(cancellationToken);
        var hasRestaurants = await _dbContext.Restaurants.AnyAsync(cancellationToken);
        var hasMenuItems = await _dbContext.MenuItems.AnyAsync(cancellationToken);

        if (!hasUsers || !hasRestaurants || !hasMenuItems)
        {
            _logger.LogWarning("Cannot seed orders: missing required dependencies (Users: {HasUsers}, Restaurants: {HasRestaurants}, MenuItems: {HasMenuItems})", 
                hasUsers, hasRestaurants, hasMenuItems);
            return false;
        }
        return true;
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var options = context.Configuration.GetOrderSeedingOptions();
        _logger.LogInformation("[Order] Starting order seeding with {OrdersPerRestaurant} orders per restaurant", options.OrdersPerRestaurant);

        try
        {
            // Load dependencies using repositories
            var dependencies = await LoadDependenciesAsync(cancellationToken);
            if (dependencies.Users.Count == 0 || dependencies.Restaurants.Count == 0)
            {
                _logger.LogWarning("[Order] No users or restaurants found - skipping order seeding");
                return Result.Success();
            }

            var seededOrders = new List<OrderAggregate>();
            var totalOrdersCreated = 0;

            foreach (var restaurant in dependencies.Restaurants)
            {   
                var restaurantOrders = await CreateOrdersForRestaurantAsync(
                    restaurant, 
                    dependencies, 
                    options, 
                    cancellationToken);

                seededOrders.AddRange(restaurantOrders);
                totalOrdersCreated += restaurantOrders.Count;

                _logger.LogInformation("[Order] Created {OrderCount} orders for {RestaurantName}", 
                    restaurantOrders.Count, restaurant.Name);
            }

            // Save all changes to the database
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Store seeded orders for potential use by other seeders (e.g., Review seeder)
            context.SharedData["SeededOrders"] = seededOrders;

            _logger.LogInformation("[Order] Successfully completed order seeding: {TotalOrders} orders created across {RestaurantCount} restaurants",
                totalOrdersCreated, dependencies.Restaurants.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Order] Failed to seed orders");
            return Result.Failure(Error.Failure("OrderSeeding.Failed", "Failed to seed orders"));
        }
    }

    private async Task<SeedingDependencies> LoadDependenciesAsync(CancellationToken cancellationToken)
    {
        // Load domain users directly from DbContext for seeding
        // Note: Take without OrderBy is acceptable for seeding as we just need any N records
        var activeUsers = await _dbContext.DomainUsers
            // .Take(100) // Limit for seeding
            .OrderBy(u => u.Created) 
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Load restaurants directly from DbContext for seeding
        var activeRestaurants = await _dbContext.Restaurants
            // .Take(20) // Limit for seeding
            .AsSplitQuery()
            .OrderBy(r => r.Created) 
            .ToListAsync(cancellationToken);

        // Filter restaurants that have menu items
        var restaurantsWithMenuItems = new List<Restaurant>();
        foreach (var restaurant in activeRestaurants)
        {
            var hasMenuItems = await _dbContext.MenuItems
                .AnyAsync(mi => mi.RestaurantId == restaurant.Id && mi.IsAvailable, cancellationToken);
            if (hasMenuItems)
            {
                restaurantsWithMenuItems.Add(restaurant);
            }
        }

        // Load coupons directly from DbContext for seeding
        var activeCoupons = await _dbContext.Coupons
            .Where(c => c.IsEnabled)
            // .Take(50) // Limit for seeding
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new SeedingDependencies(activeUsers, restaurantsWithMenuItems, activeCoupons);
    }

    private async Task<List<OrderAggregate>> CreateOrdersForRestaurantAsync(
        Restaurant restaurant,
        SeedingDependencies dependencies,
        OrderSeedingOptions options,
        CancellationToken cancellationToken)
    {
        var orders = new List<OrderAggregate>();
        
        // Load available menu items for this restaurant
        var availableMenuItems = await _menuItemRepository.GetByRestaurantIdAsync(restaurant.Id, cancellationToken);
        var availableItems = availableMenuItems.Where(mi => mi.IsAvailable).ToList();
        
        if (availableItems.Count == 0)
        {
            _logger.LogWarning("[Order] Restaurant {RestaurantName} has no available menu items - skipping", restaurant.Name);
            return orders;
        }

        for (int i = 0; i < options.OrdersPerRestaurant; i++)
        {
            try
            {
                var scenario = GenerateOrderScenario(restaurant, availableItems, dependencies, options);
                var orderResult = await CreateOrderDirectlyAsync(scenario, cancellationToken);
                
                if (orderResult.IsSuccess)
                {
                    TransitionOrderToTargetStatus(orderResult.Value, scenario.TargetStatus, scenario.OrderTimestamp);
                    orders.Add(orderResult.Value);
                }
                else
                {
                    _logger.LogWarning("[Order] Failed to create order {OrderIndex} for restaurant {RestaurantName}: {Error}", 
                        i + 1, restaurant.Name, orderResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Order] Error creating order {OrderIndex} for restaurant {RestaurantName}", 
                    i + 1, restaurant.Name);
            }
        }

        return orders;
    }

    private async Task<Result<OrderAggregate>> CreateOrderDirectlyAsync(OrderScenario scenario, CancellationToken cancellationToken)
    {
        try
        {
            // Create order items from the scenario
            var orderItems = await CreateOrderItemsAsync(scenario.Items, scenario.Restaurant.Id, cancellationToken);
            if (orderItems.Count == 0)
            {
                return Result.Failure<OrderAggregate>(Error.Validation("OrderSeeding.NoItems", "No valid order items could be created"));
            }

            // Calculate subtotal using financial service
            var subtotal = _orderFinancialService.CalculateSubtotal(orderItems);

            // Apply coupon if provided
            Money discountAmount = Money.Zero(subtotal.Currency);
            CouponId? appliedCouponId = null;
            
            if (!string.IsNullOrEmpty(scenario.CouponCode))
            {
                var coupon = await _couponRepository.GetByCodeAsync(scenario.CouponCode, scenario.Restaurant.Id, cancellationToken);
                if (coupon is not null)
                {
                    var discountResult = _orderFinancialService.ValidateAndCalculateDiscount(coupon, orderItems, subtotal);
                    if (discountResult.IsSuccess)
                    {
                        discountAmount = discountResult.Value;
                        appliedCouponId = coupon.Id;
                    }
                }
            }

            // Calculate other amounts - create new Money instances to avoid entity tracking issues
            var deliveryFee = new Money(15000m, subtotal.Currency); // Standard delivery fee
            var tipAmount = scenario.TipAmount?.Currency == subtotal.Currency 
                ? scenario.TipAmount 
                : new Money(scenario.TipAmount?.Amount ?? 0m, subtotal.Currency); // Ensure same currency
            var taxAmount = new Money(subtotal.Amount * 0.08m, subtotal.Currency); // 8% tax rate

            // Calculate final total
            var totalAmount = _orderFinancialService.CalculateFinalTotal(subtotal, discountAmount, deliveryFee, tipAmount, taxAmount);

            // Create delivery address
            var deliveryAddressResult = DeliveryAddress.Create(
                scenario.DeliveryAddress.Street,
                scenario.DeliveryAddress.City,
                scenario.DeliveryAddress.State,
                scenario.DeliveryAddress.ZipCode,
                scenario.DeliveryAddress.Country);

            if (deliveryAddressResult.IsFailure)
            {
                return Result.Failure<OrderAggregate>(deliveryAddressResult.Error);
            }

            // Determine payment method type
            if (!Enum.TryParse<PaymentMethodType>(scenario.PaymentMethod, true, out var paymentMethodType))
            {
                return Result.Failure<OrderAggregate>(Error.Validation("OrderSeeding.InvalidPaymentMethod", "Invalid payment method"));
            }

            // Create order using domain factory method (pattern from InitiateOrderCommandHandler)
            var orderResult = OrderAggregate.Create(
                OrderId.CreateUnique(),
                scenario.User.Id,
                scenario.Restaurant.Id,
                deliveryAddressResult.Value,
                orderItems,
                scenario.SpecialInstructions ?? string.Empty,
                subtotal,
                discountAmount,
                deliveryFee,
                tipAmount,
                taxAmount,
                totalAmount,
                paymentMethodType,
                appliedCouponId,
                paymentMethodType == PaymentMethodType.CashOnDelivery ? null : Guid.NewGuid().ToString(),
                null,
                scenario.OrderTimestamp);

            if (orderResult.IsFailure)
            {
                return Result.Failure<OrderAggregate>(orderResult.Error);
            }

            // Save to repository
            await _orderRepository.AddAsync(orderResult.Value, cancellationToken);

            return Result.Success(orderResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Order] Exception occurred while creating order directly");
            return Result.Failure<OrderAggregate>(Error.Failure("OrderCreation.Exception", "Exception occurred during order creation"));
        }
    }

    private OrderScenario GenerateOrderScenario(
        Restaurant restaurant,
        List<MenuItem> availableMenuItems,
        SeedingDependencies dependencies,
        OrderSeedingOptions options)
    {
        var random = new Random();
        
        // Select random user
        var user = dependencies.Users[random.Next(dependencies.Users.Count)];
        
        // Determine target status based on distribution
        var targetStatus = SelectStatusByDistribution(options.StatusDistribution);
        
        // Generate realistic timestamp for the target status
        var orderTimestamp = options.CreateRealisticTimestamps 
            ? SeedingDataGenerator.GenerateTimestampForOrderStatus(targetStatus.ToString(), options.OrderHistoryDays)
            : DateTime.UtcNow;
        
        // Select payment method
        var isOnlinePayment = random.NextDouble() < (double)(options.OnlinePaymentPercentage / 100m);
        var paymentMethod = isOnlinePayment ? "CreditCard" : "CashOnDelivery";
        
        // Generate menu item selections
        var items = GenerateMenuItemSelections(availableMenuItems, options, random);
        
        // Generate delivery address
        var deliveryAddress = SeedingDataGenerator.GenerateRandomDeliveryAddress();
        
        // Generate special instructions
        var specialInstructions = options.GenerateSpecialInstructions 
            ? SeedingDataGenerator.GenerateSpecialInstructions()
            : null;
        
        // Apply coupon randomly
        string? couponCode = null;
        if (random.NextDouble() < (double)(options.CouponUsagePercentage / 100m))
        {
            var validCoupons = dependencies.Coupons
                .Where(c => c.RestaurantId == restaurant.Id && IsValidForUse(c, orderTimestamp))
                .ToList();
            
            if (validCoupons.Any())
            {
                couponCode = validCoupons[random.Next(validCoupons.Count)].Code;
            }
        }
        
        // Generate tip amount as Money with consistent currency
        Money? tipAmount = null;
        if (random.NextDouble() < (double)(options.TipPercentage / 100m))
        {
            var tipDecimal = SeedingDataGenerator.GenerateTipAmount() ?? 0m;
            // Use the same currency as menu items (will be validated against subtotal later)
            // Default to USD if no menu items have currency info
            var currency = availableMenuItems.FirstOrDefault()?.BasePrice.Currency ?? "USD";
            tipAmount = new Money(tipDecimal, currency);
        }

        return new OrderScenario
        {
            User = user,
            Restaurant = restaurant,
            Items = items,
            DeliveryAddress = new DeliveryAddressData
            {
                Street = deliveryAddress.Street,
                City = deliveryAddress.City,
                State = deliveryAddress.State,
                ZipCode = deliveryAddress.ZipCode,
                Country = deliveryAddress.Country
            },
            PaymentMethod = paymentMethod,
            SpecialInstructions = specialInstructions,
            CouponCode = couponCode,
            TipAmount = tipAmount,
            TargetStatus = targetStatus,
            OrderTimestamp = orderTimestamp
        };
    }

    private async Task<List<OrderItem>> CreateOrderItemsAsync(List<MenuItemSelection> menuItemSelections, RestaurantId restaurantId, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItem>();

        foreach (var selection in menuItemSelections)
        {
            // Get the full menu item from repository
            var menuItem = await _menuItemRepository.GetByIdAsync(selection.MenuItemId, cancellationToken);
            if (menuItem is null)
                continue;

            // Create a copy of the Money value object to avoid EF Core tracking issues
            // Don't reuse the same Money instance from MenuItem in OrderItem
            var basePriceCopy = new Money(menuItem.BasePrice.Amount, menuItem.BasePrice.Currency);

            // Create order item using domain factory method
            var orderItemResult = OrderItem.Create(
                menuItem.MenuCategoryId,
                menuItem.Id,
                menuItem.Name,
                basePriceCopy,
                selection.Quantity,
                new List<OrderItemCustomization>() // For seeding, we'll keep customizations empty
            );

            if (orderItemResult.IsSuccess)
            {
                orderItems.Add(orderItemResult.Value);
            }
        }

        return orderItems;
    }

    private List<MenuItemSelection> GenerateMenuItemSelections(List<MenuItem> availableMenuItems, OrderSeedingOptions options, Random random)
    {
        var itemCount = random.Next(options.MinItemsPerOrder, options.MaxItemsPerOrder + 1);
        var selectedMenuItems = SeedingDataGenerator.SelectRandomItems(availableMenuItems, itemCount, itemCount);
        
        var selections = new List<MenuItemSelection>();
        
        foreach (var menuItem in selectedMenuItems)
        {
            var quantity = SeedingDataGenerator.GenerateQuantity();
            
            var selection = new MenuItemSelection
            {
                MenuItemId = menuItem.Id,
                Quantity = quantity,
                SpecialInstructions = null // For simplicity in seeding
            };
            
            selections.Add(selection);
        }
        
        return selections;
    }

    private OrderStatus SelectStatusByDistribution(Dictionary<string, int> distribution)
    {
        var weightedStatuses = new Dictionary<OrderStatus, int>();
        
        foreach (var kvp in distribution)
        {
            if (Enum.TryParse<OrderStatus>(kvp.Key, out var status))
            {
                weightedStatuses[status] = kvp.Value;
            }
        }
        
        return SeedingDataGenerator.SelectByWeight(weightedStatuses);
    }

    private bool IsValidForUse(Coupon coupon, DateTime orderTime)
    {
        // Simple validation - can be enhanced with more business logic
        return orderTime >= coupon.ValidityStartDate && 
               orderTime <= coupon.ValidityEndDate &&
               coupon.IsEnabled;
    }

    private void TransitionOrderToTargetStatus(OrderAggregate order, OrderStatus targetStatus, DateTime orderTimestamp)
    {
        if (order.Status == targetStatus)
            return;

        try
        {
            // Transition through intermediate states if necessary
            TransitionOrderToStatus(order, targetStatus, orderTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Order] Failed to transition order {OrderId} to status {TargetStatus}", order.Id, targetStatus);
        }
    }

    private void TransitionOrderToStatus(OrderAggregate order, OrderStatus targetStatus, DateTime orderTimestamp)
    {
        switch (targetStatus)
        {
            case OrderStatus.Delivered:
                // Transition through required states for delivered orders
                var estimatedDelivery = orderTimestamp.AddMinutes(-45);
                order.Accept(estimatedDelivery, orderTimestamp.AddMinutes(-40));
                order.MarkAsPreparing(orderTimestamp.AddMinutes(-25));
                order.MarkAsReadyForDelivery(orderTimestamp.AddMinutes(-10));
                order.MarkAsDelivered(orderTimestamp);
                break;
            case OrderStatus.ReadyForDelivery:
                // Transition through required states for ready orders
                var estimatedDeliveryReady = orderTimestamp.AddMinutes(-35);
                order.Accept(estimatedDeliveryReady, orderTimestamp.AddMinutes(-30));
                order.MarkAsPreparing(orderTimestamp.AddMinutes(-15));
                order.MarkAsReadyForDelivery(orderTimestamp);
                break;
            case OrderStatus.Preparing:
                // Transition through required states for preparing orders
                var estimatedDeliveryPreparing = orderTimestamp.AddMinutes(-20);
                order.Accept(estimatedDeliveryPreparing, orderTimestamp.AddMinutes(-15));
                order.MarkAsPreparing(orderTimestamp);
                break;
            case OrderStatus.Accepted:
                var estimatedDeliveryAccepted = orderTimestamp.AddMinutes(30 + new Random().Next(0, 30));
                order.Accept(estimatedDeliveryAccepted, orderTimestamp);
                break;
            case OrderStatus.Cancelled:
                order.Cancel(orderTimestamp);
                break;
            case OrderStatus.Rejected:
                order.Reject(orderTimestamp);
                break;
        }
    }


}

/// <summary>
/// Represents the dependencies needed for order seeding.
/// </summary>
public record SeedingDependencies(
    List<User> Users,
    List<Restaurant> Restaurants,
    List<Coupon> Coupons
);

/// <summary>
/// Represents a scenario for creating an order during seeding.
/// </summary>
public class OrderScenario
{
    public required User User { get; set; }
    public required Restaurant Restaurant { get; set; }
    public required List<MenuItemSelection> Items { get; set; }
    public required DeliveryAddressData DeliveryAddress { get; set; }
    public required string PaymentMethod { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? CouponCode { get; set; }
    public Money? TipAmount { get; set; }
    public required OrderStatus TargetStatus { get; set; }
    public required DateTime OrderTimestamp { get; set; }
}

/// <summary>
/// Represents a menu item selection for order seeding.
/// </summary>
public class MenuItemSelection
{
    public required MenuItemId MenuItemId { get; set; }
    public required int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }
}

/// <summary>
/// Represents delivery address data for order seeding.
/// </summary>
public class DeliveryAddressData
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string ZipCode { get; set; }
    public required string Country { get; set; }
}
