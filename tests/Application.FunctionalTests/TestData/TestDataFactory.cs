using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.TestData;

/// <summary>
/// Centralized factory for creating and managing default test data.
/// Ensures test data is created once per test suite run and provides access to entity IDs.
/// </summary>
public static class TestDataFactory
{
    private static bool _isInitialized;
    private static readonly object _lock = new();

    #region Entity IDs and References

    /// <summary>
    /// The ID of the default test customer.
    /// </summary>
    public static Guid DefaultCustomerId { get; private set; }

    /// <summary>
    /// The ID of the default test restaurant.
    /// </summary>
    public static Guid DefaultRestaurantId { get; private set; }

    /// <summary>
    /// The ID of the default test menu.
    /// </summary>
    public static Guid DefaultMenuId { get; private set; }

    /// <summary>
    /// Dictionary of menu category IDs by category name.
    /// </summary>
    public static Dictionary<string, Guid> MenuCategoryIds { get; } = new();

    /// <summary>
    /// Dictionary of menu item IDs by item name for easy access in tests.
    /// </summary>
    public static Dictionary<string, Guid> MenuItemIds { get; } = new();

    /// <summary>
    /// The ID of the default test coupon.
    /// </summary>
    public static Guid DefaultCouponId { get; private set; }

    /// <summary>
    /// The code of the default test coupon.
    /// </summary>
    public static string DefaultCouponCode => DefaultTestData.Coupon.Code;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes and seeds the database with default test data.
    /// This method is designed to be called once per test suite run.
    /// </summary>
    public static async Task InitializeAsync()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
        }

        try
        {
            await CreateTestDataAsync();
        }
        catch (Exception ex)
        {
            // Reset initialization state on failure to allow retry
            lock (_lock)
            {
                _isInitialized = false;
            }
            throw new InvalidOperationException("Failed to initialize test data factory", ex);
        }
    }

    /// <summary>
    /// Ensures test data exists in the database after a reset.
    /// If data was cleared, recreates it. If data exists, restores user context.
    /// </summary>
    public static async Task EnsureTestDataAsync()
    {
        // Don't recreate if not yet initialized
        if (!IsInitialized())
            return;

        // Check if test data still exists in the database
        // Use RestaurantId.Create() to match the entity's primary key type
        var restaurant = await FindAsync<Restaurant>(RestaurantId.Create(DefaultRestaurantId));
        
        if (restaurant is null)
        {
            // Test data was cleared, recreate it
            await CreateTestDataAsync();
        }
        else
        {
            // Data exists but user context might be lost, restore user context
            SetUserId(DefaultCustomerId);
        }
    }

    /// <summary>
    /// Creates the complete test data set.
    /// </summary>
    private static async Task CreateTestDataAsync()
    {
        // 1. Create and save the default test user
        await CreateDefaultUserAsync();

        // 2. Create and save the default restaurant
        await CreateDefaultRestaurantAsync();

        // 3. Create and save the default menu and categories
        await CreateDefaultMenuAsync();

        // 4. Create and save menu items
        await CreateDefaultMenuItemsAsync();

        // 5. Create and save the default coupon
        await CreateDefaultCouponAsync();
    }

    /// <summary>
    /// Resets the factory's state. Should be called after all tests have run.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _isInitialized = false;
            MenuCategoryIds.Clear();
            MenuItemIds.Clear();
            DefaultCustomerId = Guid.Empty;
            DefaultRestaurantId = Guid.Empty;
            DefaultMenuId = Guid.Empty;
            DefaultCouponId = Guid.Empty;
        }
    }

    #endregion

    #region Private Creation Methods

    /// <summary>
    /// Creates the default test user.
    /// </summary>
    private static async Task CreateDefaultUserAsync()
    {
        // Ensure the all roles exists
        await EnsureRolesExistAsync(DefaultTestData.User.AllRoles);

        // Create the default user and set as current user
        DefaultCustomerId = await RunAsUserAsync(
            DefaultTestData.User.Email,
            DefaultTestData.User.Password,
            DefaultTestData.User.UserRoles);
    }

    /// <summary>
    /// Creates the default test restaurant.
    /// </summary>
    private static async Task CreateDefaultRestaurantAsync()
    {
        // Create restaurant address
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            DefaultTestData.Restaurant.Address.Street,
            DefaultTestData.Restaurant.Address.City,
            DefaultTestData.Restaurant.Address.State,
            DefaultTestData.Restaurant.Address.ZipCode,
            DefaultTestData.Restaurant.Address.Country);

        if (addressResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant address: {addressResult.Error}");

        // Create restaurant contact info
        var contactInfoResult = ContactInfo.Create(
            DefaultTestData.Restaurant.Contact.Phone,
            DefaultTestData.Restaurant.Contact.Email);

        if (contactInfoResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant contact info: {contactInfoResult.Error}");

        // Create business hours
        var businessHoursResult = BusinessHours.Create(
            DefaultTestData.Restaurant.Hours.BusinessHours);

        if (businessHoursResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant business hours: {businessHoursResult.Error}");

        // Create the restaurant entity
        var restaurantResult = Restaurant.Create(
            DefaultTestData.Restaurant.Name,
            DefaultTestData.Restaurant.LogoUrl,
            DefaultTestData.Restaurant.Description,
            DefaultTestData.Restaurant.CuisineType,
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value);

        if (restaurantResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant: {restaurantResult.Error}");

        var restaurant = restaurantResult.Value;

        // Verify and activate the restaurant to accept orders
        restaurant.Verify();
        restaurant.AcceptOrders();

        // Save to database
        await AddAsync(restaurant);

        DefaultRestaurantId = restaurant.Id.Value;
    }

    /// <summary>
    /// Creates the default test menu and categories.
    /// </summary>
    private static async Task CreateDefaultMenuAsync()
    {
        // Create the main menu
        var menuResult = Menu.Create(
            RestaurantId.Create(DefaultRestaurantId),
            DefaultTestData.Menu.Name,
            DefaultTestData.Menu.Description);

        if (menuResult.IsFailure)
            throw new InvalidOperationException($"Failed to create menu: {menuResult.Error}");

        var menu = menuResult.Value;
        await AddAsync(menu);
        DefaultMenuId = menu.Id.Value;

        // Create menu categories
        var categories = new[]
        {
            DefaultTestData.Menu.Categories.MainDishes,
            DefaultTestData.Menu.Categories.Appetizers,
            DefaultTestData.Menu.Categories.Desserts,
            DefaultTestData.Menu.Categories.Beverages
        };

        foreach (var (name, sortOrder) in categories)
        {
            var menuCategoryResult = MenuCategory.Create(
                menu.Id,
                name,
                sortOrder);

            if (menuCategoryResult.IsFailure)
                throw new InvalidOperationException($"Failed to create menu category '{name}': {menuCategoryResult.Error}");

            var menuCategory = menuCategoryResult.Value;
            await AddAsync(menuCategory);
            MenuCategoryIds[name] = menuCategory.Id.Value;
        }
    }

    /// <summary>
    /// Creates the default test menu items.
    /// </summary>
    private static async Task CreateDefaultMenuItemsAsync()
    {
        var restaurantId = RestaurantId.Create(DefaultRestaurantId);

        // Create main dishes
        await CreateMenuItemAsync(
            restaurantId,
            "Main Dishes",
            DefaultTestData.MenuItems.MainDishes.ClassicBurger);

        await CreateMenuItemAsync(
            restaurantId,
            "Main Dishes",
            DefaultTestData.MenuItems.MainDishes.MargheritaPizza);

        await CreateMenuItemAsync(
            restaurantId,
            "Main Dishes",
            DefaultTestData.MenuItems.MainDishes.GrilledSalmon);

        // Create appetizers
        await CreateMenuItemAsync(
            restaurantId,
            "Appetizers",
            DefaultTestData.MenuItems.Appetizers.BuffaloWings);

        await CreateMenuItemAsync(
            restaurantId,
            "Appetizers",
            DefaultTestData.MenuItems.Appetizers.CaesarSalad);

        // Create desserts
        await CreateMenuItemAsync(
            restaurantId,
            "Desserts",
            DefaultTestData.MenuItems.Desserts.ChocolateCake);

        // Create beverages
        await CreateMenuItemAsync(
            restaurantId,
            "Beverages",
            DefaultTestData.MenuItems.Beverages.CraftBeer);

        await CreateMenuItemAsync(
            restaurantId,
            "Beverages",
            DefaultTestData.MenuItems.Beverages.FreshJuice);
    }

    /// <summary>
    /// Helper method to create a menu item.
    /// </summary>
    private static async Task CreateMenuItemAsync(
        RestaurantId restaurantId,
        string categoryName,
        (string Name, string Description, decimal Price) itemData)
    {
        if (!MenuCategoryIds.TryGetValue(categoryName, out var categoryId))
            throw new InvalidOperationException($"Menu category '{categoryName}' not found");

        var (name, description, price) = itemData;

        var menuItemResult = MenuItem.Create(
            restaurantId,
            MenuCategoryId.Create(categoryId),
            name,
            description,
            new Money(price, DefaultTestData.Currency.Default));

        if (menuItemResult.IsFailure)
            throw new InvalidOperationException($"Failed to create menu item '{name}': {menuItemResult.Error}");

        var menuItem = menuItemResult.Value;
        await AddAsync(menuItem);
        MenuItemIds[name] = menuItem.Id.Value;
    }

    /// <summary>
    /// Creates the default test coupon.
    /// </summary>
    private static async Task CreateDefaultCouponAsync()
    {
        var restaurantId = RestaurantId.Create(DefaultRestaurantId);

        // Create coupon value (percentage discount)
        var couponValueResult = CouponValue.CreatePercentage(
            DefaultTestData.Coupon.DiscountPercentage);

        if (couponValueResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon value: {couponValueResult.Error}");

        // Create applies to (whole order)
        var appliesToResult = AppliesTo.CreateForWholeOrder();

        if (appliesToResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon applies to: {appliesToResult.Error}");

        // Create minimum order amount
        var minimumOrderAmount = new Money(
            DefaultTestData.Coupon.MinimumOrderAmount,
            DefaultTestData.Currency.Default);

        // Create the coupon
        var couponResult = Coupon.Create(
            restaurantId,
            DefaultTestData.Coupon.Code,
            DefaultTestData.Coupon.Description,
            couponValueResult.Value,
            appliesToResult.Value,
            DateTime.UtcNow.AddDays(-1), // Start yesterday (valid)
            DateTime.UtcNow.AddDays(DefaultTestData.Coupon.ValidDaysFromNow), // End in specified days
            minimumOrderAmount,
            totalUsageLimit: DefaultTestData.Coupon.TotalUsageLimit,
            usageLimitPerUser: DefaultTestData.Coupon.UsageLimitPerUser,
            isEnabled: true);

        if (couponResult.IsFailure)
            throw new InvalidOperationException($"Failed to create coupon: {couponResult.Error}");

        var coupon = couponResult.Value;
        await AddAsync(coupon);
        DefaultCouponId = coupon.Id.Value;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the menu item ID by name. Throws an exception if not found.
    /// </summary>
    /// <param name="itemName">The name of the menu item.</param>
    /// <returns>The menu item ID.</returns>
    public static Guid GetMenuItemId(string itemName)
    {
        if (!MenuItemIds.TryGetValue(itemName, out var itemId))
        {
            throw new ArgumentException($"Menu item '{itemName}' not found in test data. Available items: {string.Join(", ", MenuItemIds.Keys)}");
        }
        return itemId;
    }

    /// <summary>
    /// Gets multiple menu item IDs by names.
    /// </summary>
    /// <param name="itemNames">The names of the menu items.</param>
    /// <returns>A list of menu item IDs.</returns>
    public static List<Guid> GetMenuItemIds(params string[] itemNames)
    {
        return itemNames.Select(GetMenuItemId).ToList();
    }

    /// <summary>
    /// Gets the menu category ID by name. Throws an exception if not found.
    /// </summary>
    /// <param name="categoryName">The name of the menu category.</param>
    /// <returns>The menu category ID.</returns>
    public static Guid GetMenuCategoryId(string categoryName)
    {
        if (!MenuCategoryIds.TryGetValue(categoryName, out var categoryId))
        {
            throw new ArgumentException($"Menu category '{categoryName}' not found in test data. Available categories: {string.Join(", ", MenuCategoryIds.Keys)}");
        }
        return categoryId;
    }

    /// <summary>
    /// Checks if the test data factory has been initialized.
    /// </summary>
    /// <returns>True if initialized, false otherwise.</returns>
    public static bool IsInitialized()
    {
        lock (_lock)
        {
            return _isInitialized;
        }
    }

    /// <summary>
    /// Creates an inactive restaurant (not verified and not accepting orders) for testing inactive restaurant validation.
    /// </summary>
    /// <returns>The ID of the created inactive restaurant.</returns>
    public static async Task<Guid> CreateInactiveRestaurantAsync()
    {
        // Create restaurant address
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            "456 Inactive Street",
            "Test City",
            "TS",
            "54321",
            "US");

        if (addressResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant address: {addressResult.Error}");

        // Create restaurant contact info
        var contactInfoResult = ContactInfo.Create(
            "+1-555-0123",
            "inactive@restaurant.com");

        if (contactInfoResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant contact info: {contactInfoResult.Error}");

        // Create business hours
        var businessHoursResult = BusinessHours.Create("Mon-Sun: 9:00 AM - 10:00 PM");

        if (businessHoursResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant business hours: {businessHoursResult.Error}");

        // Create the restaurant entity
        var restaurantResult = Restaurant.Create(
            "Inactive Test Restaurant",
            "https://example.com/inactive-logo.jpg",
            "A test restaurant that is not accepting orders",
            "Test Cuisine",
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value);

        if (restaurantResult.IsFailure)
            throw new InvalidOperationException($"Failed to create restaurant: {restaurantResult.Error}");

        var restaurant = restaurantResult.Value;

        // DO NOT verify or activate the restaurant - this makes it inactive
        // restaurant.Verify();     // <- Not called, so IsVerified = false
        // restaurant.AcceptOrders(); // <- Not called, so IsAcceptingOrders = false

        // Save to database
        await AddAsync(restaurant);

        return restaurant.Id.Value;
    }

    /// <summary>
    /// Creates a second restaurant with menu items for testing cross-restaurant validation.
    /// </summary>
    /// <returns>A tuple containing the restaurant ID and a menu item ID from that restaurant.</returns>
    public static async Task<(Guid RestaurantId, Guid MenuItemId)> CreateSecondRestaurantWithMenuItemsAsync()
    {
        // Create second restaurant address
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            "789 Second Street",
            "Test City",
            "TS",
            "67890",
            "US");

        if (addressResult.IsFailure)
            throw new InvalidOperationException($"Failed to create second restaurant address: {addressResult.Error}");

        // Create second restaurant contact info
        var contactInfoResult = ContactInfo.Create(
            "+1-555-0456",
            "second@restaurant.com");

        if (contactInfoResult.IsFailure)
            throw new InvalidOperationException($"Failed to create second restaurant contact info: {contactInfoResult.Error}");

        // Create business hours
        var businessHoursResult = BusinessHours.Create("Mon-Sun: 8:00 AM - 11:00 PM");

        if (businessHoursResult.IsFailure)
            throw new InvalidOperationException($"Failed to create second restaurant business hours: {businessHoursResult.Error}");

        // Create the second restaurant entity
        var restaurantResult = Restaurant.Create(
            "Second Test Restaurant",
            "https://example.com/second-logo.jpg",
            "A second test restaurant with different menu items",
            "Different Cuisine",
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value);

        if (restaurantResult.IsFailure)
            throw new InvalidOperationException($"Failed to create second restaurant: {restaurantResult.Error}");

        var restaurant = restaurantResult.Value;

        // Verify and activate the restaurant
        restaurant.Verify();
        restaurant.AcceptOrders();

        // Save restaurant to database
        await AddAsync(restaurant);

        // Create a menu category for the second restaurant
        var menuCategoryResult = MenuCategory.Create(
            MenuId.CreateUnique(),
            "Second Restaurant Items",
            1);

        if (menuCategoryResult.IsFailure)
            throw new InvalidOperationException($"Failed to create menu category: {menuCategoryResult.Error}");

        var menuCategory = menuCategoryResult.Value;
        await AddAsync(menuCategory);

        // Create a menu item for the second restaurant
        var menuItemResult = MenuItem.Create(
            restaurant.Id,
            menuCategory.Id,
            "Special Pizza",
            "A special pizza only available at the second restaurant",
            new Money(18.99m, "USD"));

        if (menuItemResult.IsFailure)
            throw new InvalidOperationException($"Failed to create menu item: {menuItemResult.Error}");

        var menuItem = menuItemResult.Value;
        await AddAsync(menuItem);

        return (restaurant.Id.Value, menuItem.Id.Value);
    }

    /// <summary>
    /// Marks a specific menu item as unavailable for testing unavailable menu item validation.
    /// </summary>
    /// <param name="itemName">The name of the menu item to mark as unavailable.</param>
    /// <returns>The name of the menu item that was marked as unavailable.</returns>
    public static async Task<string> MarkMenuItemAsUnavailableAsync(string itemName)
    {
        var menuItemGuid = GetMenuItemId(itemName);
        var menuItemId = MenuItemId.Create(menuItemGuid);
        
        var menuItem = await FindAsync<MenuItem>(menuItemId);
        menuItem!.MarkAsUnavailable();
        
        // Update the menu item in the database
        await UpdateAsync(menuItem);
        
        return itemName;
    }

    #endregion
}
