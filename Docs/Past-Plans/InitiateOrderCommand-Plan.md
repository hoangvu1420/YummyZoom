# Implementation Plan for InitiateOrderCommand

## 1. Overview

The `InitiateOrderCommand` will be responsible for initiating a new order in the system. It will:
1. Validate the input data
2. Calculate the financial details
3. Create a payment intent with Stripe (if online payment)
4. Create an Order aggregate with appropriate status
5. Return the OrderId and ClientSecret (if online payment)

## 2. File Structure

I'll create the following files:

```
src/Application/Orders/Commands/InitiateOrder/
├── InitiateOrderCommand.cs
├── InitiateOrderCommandHandler.cs
├── InitiateOrderCommandValidator.cs
├── InitiateOrderResponse.cs
└── OrderErrors.cs (if not already exists)
```

## 3. Implementation Details

### 3.1. InitiateOrderCommand

The command will include:
- CustomerId
- RestaurantId
- List of OrderItemDto (with MenuItemId, Quantity, and Customizations)
- DeliveryAddress details
- Special instructions (optional)
- Tip amount (optional)
- PaymentMethodType (CreditCard, PayPal, ApplePay, GooglePay, or CashOnDelivery)
- CouponId (optional)

### 3.2. InitiateOrderResponse

The response will include:
- OrderId
- ClientSecret (only for online payments)

### 3.3. InitiateOrderCommandHandler

The handler will:
1. Validate the command using the validator
2. Fetch the restaurant and menu items from repositories
3. Calculate the financial details (subtotal, tax, delivery fee, discount, total)
4. If payment method is online:
   - Call `_paymentGatewayService.CreatePaymentIntentAsync()` with the total amount and metadata
   - Create an Order with `Order.Create()`, providing the payment intent ID and setting status to `AwaitingPayment`
5. If payment is COD:
   - Create an Order with `Order.Create()`, setting status to `Placed` (handled internally by the Order aggregate)
6. Add the Order to the repository and save changes
7. Return OrderId and ClientSecret (if online payment)

### 3.4. InitiateOrderCommandValidator

The validator will:
1. Ensure CustomerId is valid
2. Ensure RestaurantId is valid
3. Ensure OrderItems is not empty
4. Validate DeliveryAddress fields
5. Validate TipAmount is non-negative
6. Validate PaymentMethodType is a valid enum value

## 4. Dependencies

The handler will require:
- IOrderRepository
- IRestaurantRepository
- IMenuItemRepository
- IPaymentGatewayService
- IUnitOfWork
- IUser (for current user information)
- ILogger

## 5. Authorization

The command will be decorated with the `[Authorize]` attribute to ensure only authenticated users can place orders.

## 6. Error Handling

The handler will handle various error scenarios:
- Invalid input data (through validator)
- Restaurant not found or not active
- Menu items not found or not available
- Payment gateway errors
- Database transaction errors

## 7. Testing Considerations

Unit tests will cover:
- Command validation
- Handler logic for both online and COD payment flows
- Error handling scenarios

## 8. Integration with Stripe

The handler will create a payment intent with Stripe for online payments, passing:
- Amount: The total order amount
- Currency: USD (or configured currency)
- Metadata: Including OrderId and CustomerId for webhook processing
- Automatic payment methods: Enabled

The client secret returned by Stripe will be included in the response for the frontend to complete the payment.

## 9. Repository Interfaces and Implementations

### 9.1. Repository Interfaces

#### 9.1.1. IOrderRepository

```csharp
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IOrderRepository
{
    /// <summary>
    /// Adds a new order to the repository.
    /// </summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an order by its ID.
    /// </summary>
    Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an order by its payment gateway reference ID.
    /// </summary>
    Task<Order?> GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing order in the repository.
    /// </summary>
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
```

#### 9.1.2. IRestaurantRepository

```csharp
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IRestaurantRepository
{
    /// <summary>
    /// Gets a restaurant by its ID.
    /// </summary>
    Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a restaurant is currently active and accepting orders.
    /// </summary>
    Task<bool> IsActiveAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
}
```

#### 9.1.3. IMenuItemRepository

```csharp
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IMenuItemRepository
{
    /// <summary>
    /// Gets a menu item by its ID.
    /// </summary>
    Task<MenuItem?> GetByIdAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets multiple menu items by their IDs.
    /// </summary>
    Task<List<MenuItem>> GetByIdsAsync(List<MenuItemId> menuItemIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all menu items for a specific restaurant.
    /// </summary>
    Task<List<MenuItem>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a menu item is currently available.
    /// </summary>
    Task<bool> IsAvailableAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default);
}
```

### 9.2. Repository Implementations

#### 9.2.1. OrderRepository

```csharp
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrderRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetByPaymentGatewayReferenceIdAsync(string paymentGatewayReferenceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.PaymentTransactions.Any(pt => pt.PaymentGatewayReferenceId == paymentGatewayReferenceId), cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }
}
```

#### 9.2.2. RestaurantRepository

```csharp
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RestaurantRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Restaurant?> GetByIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants
            .FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken);
    }

    public async Task<bool> IsActiveAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        var restaurant = await _dbContext.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken);
        
        return restaurant != null && restaurant.IsActive;
    }
}
```

#### 9.2.3. MenuItemRepository

```csharp
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MenuItemRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<MenuItem?> GetByIdAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
    }

    public async Task<List<MenuItem>> GetByIdsAsync(List<MenuItemId> menuItemIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MenuItem>> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAvailableAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default)
    {
        var menuItem = await _dbContext.MenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
        
        return menuItem != null && menuItem.IsAvailable;
    }
}
```

### 9.3. Registration in DependencyInjection.cs

The new repositories need to be registered in the `DependencyInjection.cs` file in the Infrastructure project:

```csharp
// In src/Infrastructure/DependencyInjection.cs
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IRestaurantRepository, RestaurantRepository>();
services.AddScoped<IMenuItemRepository, MenuItemRepository>();
```

### 9.4. DbContext Configuration

The ApplicationDbContext needs to be updated to include the new entities:

```csharp
// In src/Infrastructure/Data/ApplicationDbContext.cs
public DbSet<Order> Orders { get; set; }
public DbSet<Restaurant> Restaurants { get; set; }
public DbSet<MenuItem> MenuItems { get; set; }
```

## 10. Next Steps

1. Implement the repository interfaces and their concrete implementations
2. Implement the InitiateOrderCommand and its handler
3. Create unit tests for the command and handler
4. Implement the HandleStripeWebhookCommand to process payment confirmations