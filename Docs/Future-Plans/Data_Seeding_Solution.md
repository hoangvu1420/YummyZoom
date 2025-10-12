# Data Seeding Solution for YummyZoom

## Goal

Create a comprehensive, maintainable, and domain-compliant database seeding system that provides rich, realistic test data for frontend development and testing. The solution must be idempotent, configurable, and extensible while adhering to Clean Architecture principles and domain business rules.

## Current State Analysis

The YummyZoom project currently implements a basic data seeding mechanism in `ApplicationDbContextInitialiser.cs` that seeds:

- Identity roles (Administrator, User, RestaurantOwner)
- A default administrator user  
- Two sample users with devices and FCM tokens
- One restaurant ("YummyZoom Italian") with a basic menu structure
- Sample TodoList items (template code)

### Current Limitations

1. **Limited Data Coverage**: Only covers basic user, restaurant, and menu data
2. **Hard-coded Values**: Static data values make it difficult to extend or customize
3. **Monolithic Structure**: All seeding logic is in one large class
4. **No Configuration**: Cannot easily adjust seeding behavior for different environments
5. **Missing Core Entities**: No seeding for critical business entities like orders, coupons, reviews, etc.
6. **No Realistic Relationships**: Limited cross-entity relationships for testing scenarios

## Proposed Solution Architecture

### 1. Modular Seeding Framework

Create a structured, extensible seeding system that follows the Clean Architecture principles and aligns with the existing domain model.

#### Core Components

```
Infrastructure/
├── Persistence/
│   ├── EfCore/
│   │   ├── Seeding/
│   │   │   ├── ISeeder.cs                      # Base seeder interface
│   │   │   ├── SeedingConfiguration.cs         # Configuration options
│   │   │   ├── SeedingOrchestrator.cs          # Main coordinator
│   │   │   ├── Seeders/
│   │   │   │   ├── IdentitySeeders/
│   │   │   │   │   ├── RoleSeeder.cs
│   │   │   │   │   ├── UserSeeder.cs
│   │   │   │   │   └── DeviceSeeder.cs
│   │   │   │   ├── RestaurantSeeders/
│   │   │   │   │   ├── RestaurantSeeder.cs
│   │   │   │   │   ├── MenuSeeder.cs
│   │   │   │   │   ├── MenuItemSeeder.cs
│   │   │   │   │   ├── CustomizationGroupSeeder.cs
│   │   │   │   │   └── TagSeeder.cs
│   │   │   │   ├── BusinessSeeders/
│   │   │   │   │   ├── OrderSeeder.cs
│   │   │   │   │   ├── CouponSeeder.cs
│   │   │   │   │   ├── ReviewSeeder.cs
│   │   │   │   │   ├── TeamCartSeeder.cs
│   │   │   │   │   └── SupportTicketSeeder.cs
│   │   │   │   └── AccountingSeeders/
│   │   │   │       ├── RestaurantAccountSeeder.cs
│   │   │   │       └── AccountTransactionSeeder.cs
│   │   │   ├── Data/
│   │   │   │   ├── SeedingProfiles/
│   │   │   │   │   ├── MinimalProfile.json
│   │   │   │   │   ├── DevelopmentProfile.json
│   │   │   │   │   └── ComprehensiveProfile.json
│   │   │   │   └── Templates/
│   │   │   │       ├── RestaurantTemplates.json
│   │   │   │       ├── MenuItemTemplates.json
│   │   │   │       └── UserTemplates.json
│   │   │   └── Extensions/
│   │   │       └── SeedingServiceExtensions.cs
│   │   └── ApplicationDbContextInitialiser.cs  # Updated to use new system
```

### 2. Configuration-Driven Approach

#### Seeding Configuration Options

```csharp
public class SeedingConfiguration
{
    public string Profile { get; set; } = "Development";
    public bool EnableIdempotentSeeding { get; set; } = true;
    public bool ClearExistingData { get; set; } = false;
    public bool SeedTestData { get; set; } = true;
    public Dictionary<string, bool> EnabledSeeders { get; set; } = new();
    public Dictionary<string, object> SeederSettings { get; set; } = new();
}
```

#### Seeding Profiles

**Minimal Profile**: Essential data for basic application functionality
- System roles and admin user
- 1 restaurant with basic menu (5-10 items)
- 2-3 customer users

**Development Profile**: Rich dataset for frontend development
- System roles and multiple admin/staff users
- 3-5 restaurants with full menus (20-30 items each)
- Customization groups and dietary tags
- 10-15 customer users with addresses and payment methods
- Sample orders in various states
- Reviews and ratings
- Active coupons

**Comprehensive Profile**: Full dataset for testing and demonstration
- All entities from Development Profile
- Team carts with collaborative ordering scenarios
- Support tickets in various states
- Account transactions and financial data
- Multiple restaurants per cuisine type
- Complex menu hierarchies with seasonal items

### 3. Idempotent Seeding Implementation

#### Base Seeder Interface

```csharp
public interface ISeeder
{
    string Name { get; }
    int Order { get; }
    Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default);
    Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default);
}

public class SeedingContext
{
    public ApplicationDbContext DbContext { get; }
    public SeedingConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }
    public ILogger Logger { get; }
    public Dictionary<string, object> SharedData { get; } = new();
}
```

#### Example Implementation - Restaurant Seeder

```csharp
public class RestaurantSeeder : ISeeder
{
    public string Name => "Restaurant";
    public int Order => 100;

    public async Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken)
    {
        if (!context.Configuration.EnableIdempotentSeeding)
            return true;

        // Check if restaurants already exist
        return !await context.DbContext.Restaurants.AnyAsync(cancellationToken);
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken)
    {
        var templates = await LoadRestaurantTemplatesAsync(context.Configuration.Profile);
        var seededRestaurants = new List<Restaurant>();

        foreach (var template in templates)
        {
            var restaurantResult = await CreateRestaurantFromTemplateAsync(template, context);
            if (restaurantResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to seed restaurant {Name}: {Error}", 
                    template.Name, restaurantResult.Error);
                continue;
            }

            // Clear domain events during seeding
            restaurantResult.Value.ClearDomainEvents();
            
            context.DbContext.Restaurants.Add(restaurantResult.Value);
            seededRestaurants.Add(restaurantResult.Value);
        }

        await context.DbContext.SaveChangesAsync(cancellationToken);
        
        // Store for dependent seeders
        context.SharedData["SeededRestaurants"] = seededRestaurants;
        
        context.Logger.LogInformation("Successfully seeded {Count} restaurants", seededRestaurants.Count);
        return Result.Success();
    }

    private async Task<Result<Restaurant>> CreateRestaurantFromTemplateAsync(
        RestaurantTemplate template, 
        SeedingContext context)
    {
        // Use domain factory method to ensure business rules
        var result = Restaurant.Create(
            template.Name,
            template.LogoUrl,
            template.BackgroundImageUrl,
            template.Description,
            template.CuisineType,
            template.Address.Street,
            template.Address.City,
            template.Address.State,
            template.Address.ZipCode,
            template.Address.Country,
            template.ContactInfo.Phone,
            template.ContactInfo.Email,
            template.BusinessHours,
            template.Location?.Latitude,
            template.Location?.Longitude);

        if (result.IsFailure)
            return result;

        // Apply business operations
        if (template.IsVerified)
            result.Value.Verify();
            
        if (template.IsAcceptingOrders)
            result.Value.AcceptOrders();

        return result;
    }
}
```

### 4. Domain-Compliant Data Creation

#### Leveraging Domain Factory Methods

All seeded entities must be created through their domain factory methods to ensure:
- Business rule validation
- Proper invariant enforcement  
- Consistent domain event generation (cleared during seeding)
- Value object validation

#### Example - Order Seeding with Business Logic

```csharp
public class OrderSeeder : ISeeder
{
    private readonly IOrderCalculationService _orderCalculationService;
    private readonly ICouponValidationService _couponValidationService;

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken)
    {
        var restaurants = context.SharedData["SeededRestaurants"] as List<Restaurant>;
        var users = context.SharedData["SeededUsers"] as List<User>;
        var menuItems = context.SharedData["SeededMenuItems"] as List<MenuItem>;

        foreach (var orderTemplate in await LoadOrderTemplatesAsync())
        {
            // Select entities for this order
            var restaurant = SelectRestaurant(restaurants, orderTemplate);
            var customer = SelectCustomer(users, orderTemplate);
            var items = SelectMenuItems(menuItems, restaurant.Id, orderTemplate);

            // Calculate order totals using domain service
            var calculationResult = await _orderCalculationService.CalculateOrderTotalsAsync(
                items, orderTemplate.CouponCode, restaurant.Id, customer.Id);
                
            if (calculationResult.IsFailure)
                continue;

            // Create order through domain factory
            var orderResult = Order.Create(
                customer.Id,
                restaurant.Id,
                orderTemplate.DeliveryAddress,
                orderTemplate.SpecialInstructions,
                calculationResult.Value.Subtotal,
                calculationResult.Value.DiscountAmount,
                calculationResult.Value.DeliveryFee,
                calculationResult.Value.TipAmount,
                calculationResult.Value.TaxAmount,
                calculationResult.Value.TotalAmount,
                items.Select(CreateOrderItem).ToList(),
                calculationResult.Value.AppliedCouponId);

            if (orderResult.IsFailure)
                continue;

            // Apply state transitions based on template
            ApplyOrderStateTransitions(orderResult.Value, orderTemplate);
            
            orderResult.Value.ClearDomainEvents();
            context.DbContext.Orders.Add(orderResult.Value);
        }

        await context.DbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### 5. Cross-Entity Relationship Management

#### Dependency-Aware Seeding Order

Seeders execute in dependency order to ensure referential integrity:

1. **Foundation Layer** (Order: 1-50)
   - Roles, Tags, System Data

2. **Identity Layer** (Order: 51-100)  
   - Users, Devices, Sessions

3. **Restaurant Catalog Layer** (Order: 101-200)
   - Restaurants, Menus, MenuCategories, MenuItems, CustomizationGroups

4. **Authorization Layer** (Order: 201-250)
   - RoleAssignments (requires Users + Restaurants)

5. **Business Logic Layer** (Order: 251-400)
   - Coupons, Orders, Reviews, TeamCarts

6. **Financial Layer** (Order: 401-500)
   - RestaurantAccounts, AccountTransactions

7. **Support Layer** (Order: 501-600)
   - SupportTickets

#### Shared Data Pattern

Seeders store created entities in `SeedingContext.SharedData` for dependent seeders:

```csharp
// RestaurantSeeder stores restaurants
context.SharedData["SeededRestaurants"] = restaurants;

// MenuItemSeeder uses them
var restaurants = context.SharedData["SeededRestaurants"] as List<Restaurant>;
```

### 6. Template-Based Data Definition

#### Restaurant Template Example

```json
{
  "restaurants": [
    {
      "name": "Bella Vista Italian",
      "description": "Authentic Italian cuisine in the heart of downtown",
      "cuisineType": "Italian",
      "logoUrl": "https://example.com/bella-vista-logo.png",
      "backgroundImageUrl": "https://example.com/bella-vista-bg.jpg",
      "isVerified": true,
      "isAcceptingOrders": true,
      "address": {
        "street": "123 Main Street",
        "city": "Downtown",
        "state": "CA",
        "zipCode": "90210",
        "country": "USA"
      },
      "contactInfo": {
        "phone": "+1 (555) 123-4567",
        "email": "orders@bellavista.com"
      },
      "businessHours": "11:00-22:00",
      "location": {
        "latitude": 34.0522,
        "longitude": -118.2437
      },
      "menuCategories": [
        {
          "name": "Appetizers",
          "displayOrder": 1,
          "items": [
            {
              "name": "Bruschetta Classica",
              "description": "Grilled bread topped with fresh tomatoes, garlic, and basil",
              "basePrice": 8.99,
              "imageUrl": "https://example.com/bruschetta.jpg",
              "isAvailable": true,
              "dietaryTags": ["Vegetarian"],
              "customizationGroups": ["Bread Options"]
            }
          ]
        }
      ]
    }
  ]
}
```

### 7. Configuration and Environment Integration

#### appsettings Integration

```json
{
  "Seeding": {
    "Profile": "Development",
    "EnableIdempotentSeeding": true,
    "ClearExistingData": false,
    "SeedTestData": true,
    "EnabledSeeders": {
      "Role": true,
      "User": true,
      "Restaurant": true,
      "Order": true,
      "Review": false
    },
    "SeederSettings": {
      "Restaurant": {
        "Count": 5,
        "ItemsPerRestaurant": 25
      },
      "User": {
        "CustomerCount": 15,
        "StaffCount": 5
      }
    }
  }
}
```

#### Environment-Specific Profiles

- **Development**: Rich dataset for UI development
- **Testing**: Controlled dataset for automated tests  
- **Staging**: Production-like data volume
- **Demo**: Showcase data for presentations

### 8. Integration with Existing Infrastructure

#### Updated ApplicationDbContextInitialiser

```csharp
public class ApplicationDbContextInitialiser
{
    private readonly SeedingOrchestrator _seedingOrchestrator;

    public async Task SeedAsync()
    {
        try
        {
            await _seedingOrchestrator.ExecuteSeedingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
```

#### Dependency Injection Registration

```csharp
// In Infrastructure/DependencyInjection.cs
services.Configure<SeedingConfiguration>(configuration.GetSection("Seeding"));
services.AddScoped<SeedingOrchestrator>();
services.AddScoped<ISeeder, RoleSeeder>();
services.AddScoped<ISeeder, UserSeeder>();
services.AddScoped<ISeeder, RestaurantSeeder>();
// ... register all seeders
```

### 9. Error Handling and Resilience

#### Transaction Management

- Each seeder runs in its own transaction
- Failed seeders don't block others
- Comprehensive logging for troubleshooting
- Graceful degradation for non-critical data

#### Validation and Recovery

- Pre-seeding validation checks
- Rollback capabilities for failed operations
- Detailed error reporting
- Support for partial re-seeding

### 10. Performance Considerations

#### Bulk Operations

- Use bulk insert operations where possible
- Batch processing for large datasets
- Efficient query patterns
- Memory management for large seed operations

#### Lazy Loading and Caching

- Disable change tracking during seeding
- Use appropriate database execution strategies
- Cache template data to avoid repeated file I/O

## Implementation Phases

### Phase 1: Foundation (Week 1)
**Deliverables:**
- Core seeding framework (`ISeeder`, `SeedingOrchestrator`, `SeedingConfiguration`)
- Basic template loading system
- Identity and Restaurant seeders
- Integration with existing initializer

**Acceptance Criteria:**
- [x] `ISeeder` interface and base framework implemented
- [x] `SeedingOrchestrator` executes seeders in dependency order
- [x] Configuration system loads from appsettings.json
- [x] Identity seeder creates roles, users, and devices idempotently
- [x] Restaurant seeder creates 3+ restaurants with basic data from JSON templates
- [x] Existing `ApplicationDbContextInitialiser` integrates with new system
- [x] All seeders run without errors on fresh database
- [x] Re-running seeders doesn't create duplicate data

### Phase 2: Core Business Entities (Week 2)  
**Deliverables:**
- Menu and MenuItem seeders
- CustomizationGroup and Tag seeders
- Order seeder with business logic integration
- Coupon seeder

**Acceptance Criteria:**
- [x] Menu/MenuCategory seeders create hierarchical menu structures
- [x] MenuItem seeder creates 20+ items per restaurant with proper categorization
- [x] CustomizationGroup seeder creates reusable option groups (sizes, toppings, etc.)
- [x] Tag seeder creates dietary and cuisine classification tags
- [ ] Order seeder creates orders in various states using domain calculation services
- [ ] Coupon seeder creates active promotional coupons with usage limits
- [ ] All business rules and invariants are respected during creation
- [ ] Cross-entity relationships (restaurant→menu→items) work correctly

### Phase 3: Advanced Features (Week 3)
**Deliverables:**
- Review and TeamCart seeders
- Support ticket seeder
- Restaurant account and transaction seeders
- Profile-based configuration

**Acceptance Criteria:**
- [ ] Review seeder creates realistic customer feedback linked to completed orders
- [ ] TeamCart seeder creates collaborative ordering scenarios with multiple users
- [ ] SupportTicket seeder creates tickets in various states with context links
- [ ] RestaurantAccount seeder creates financial accounts with initial balances
- [ ] AccountTransaction seeder creates audit trail of financial operations
- [ ] Multiple seeding profiles (Minimal, Development, Comprehensive) implemented
- [ ] Profile selection via configuration controls data volume and complexity
- [ ] All advanced features integrate seamlessly with core entities

### Phase 4: Polish and Documentation (Week 4)
**Deliverables:**
- Comprehensive testing
- Performance optimization
- Documentation and examples
- Error handling improvements

**Acceptance Criteria:**
- [ ] Unit tests cover all seeder implementations
- [ ] Integration tests validate end-to-end seeding process
- [ ] Performance benchmarks show acceptable seeding times (<30s for Development profile)
- [ ] Error handling gracefully manages failures without breaking other seeders
- [ ] Developer documentation with examples and configuration guide
- [ ] Migration guide from existing seeding system
- [ ] Logging provides clear progress indication and error diagnostics
- [ ] Code review and quality standards met

## Benefits

1. **Maintainability**: Modular design makes it easy to add/modify seeded data
2. **Flexibility**: Configuration-driven approach supports different environments  
3. **Reliability**: Idempotent operations prevent duplicate data issues
4. **Domain Compliance**: Uses domain factory methods ensuring business rule adherence
5. **Scalability**: Template-based system can handle complex scenarios
6. **Developer Experience**: Rich development datasets improve frontend team productivity
7. **Testing Support**: Controlled datasets enable reliable automated testing

## Migration Strategy

1. **Gradual Migration**: Keep existing seeding working while building new system
2. **Feature Flags**: Use configuration to switch between old and new seeders
3. **Validation**: Compare outputs between old and new systems
4. **Documentation**: Provide clear migration guide for team

This solution provides a robust, extensible, and maintainable approach to database seeding that aligns with the project's Clean Architecture principles while meeting all the specified requirements.