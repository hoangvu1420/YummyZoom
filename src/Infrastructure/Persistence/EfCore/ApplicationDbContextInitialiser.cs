using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Persistence.EfCore;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();

        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IIdentityService _identityService;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IDeviceRepository deviceRepository,
        IUserDeviceSessionRepository userDeviceSessionRepository)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _identityService = identityService;
        _userRepository = userRepository;
        _deviceRepository = deviceRepository;
        _userDeviceSessionRepository = userDeviceSessionRepository;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        string[] roles = [Roles.Administrator, Roles.User, Roles.RestaurantOwner];

        foreach (var roleName in roles)
        {
            if (_roleManager.Roles.All(r => r.Name != roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // Default administrator - using legacy approach for consistency with existing code
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            await _userManager.AddToRolesAsync(administrator, [Roles.Administrator]);
        }

        // Seed users with devices and sessions
        await SeedDefaultUserWithDeviceAsync("User 1", "hoangnguyenvu1420@gmail.com", "123456",
            "eaVgeLKNTgqOSMFsNmUYo5:APA91bEmTbtxCFo_888pNUNEUi6euk66GP6iYbtWV_Sq2uWeb81IPO1LMfKpsabH77N_xjdVwmNgd3ms3xTLf92iK8DVvr-Zh_bOKB-wZIm3Ns3E5T0O5Xs",
            "Android", "seed-device-1", "Seed Device 1");

        await SeedDefaultUserWithDeviceAsync("User 2", "hoangnguyenvu1220@gmail.com", "123456",
            "cjYujyIsRHGCBNHQaIsvy2:APA91bErEQrSMTKusz8AkdpswagbOwt4x2FqkTJRURMa6xg1HpcDANoxEH0RTz-J4hlC1QXOIKsjBtwBk6pn8JVWNLliQxRYtYHHVrI77doOwPOrLxHQruE",
            "Android", "seed-device-2", "Seed Device 2");

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            var todoList = TodoList.Create("Todo List", Color.White);

            var item1 = TodoItem.Create("Make a todo list 📃", null, PriorityLevel.None, null);
            item1.ClearDomainEvents();
            todoList.AddItem(item1);

            var item2 = TodoItem.Create("Check off the first item ✅", null, PriorityLevel.None, null);
            item2.ClearDomainEvents();
            todoList.AddItem(item2);

            var item3 = TodoItem.Create("Realise you've already done two things on the list! 🤯", null, PriorityLevel.None, null);
            item3.ClearDomainEvents();
            todoList.AddItem(item3);

            var item4 = TodoItem.Create("Reward yourself with a nice, long nap 🏆", null, PriorityLevel.None, null);
            item4.ClearDomainEvents();
            todoList.AddItem(item4);

            // Clear domain events to avoid producing events during seeding
            todoList.ClearDomainEvents();

            _context.TodoLists.Add(todoList);
            await _context.SaveChangesAsync();
        }

        // Seed restaurant data
        await SeedRestaurantDataAsync();

        _logger.LogInformation("Database seeding completed successfully.");
    }

    private async Task SeedDefaultUserWithDeviceAsync(string name, string email, string password, string fcmToken, string platform, string deviceId, string? modelName)
    {
        // Check if user already exists in identity system
        if (_userManager.Users.Any(u => u.Email == email))
        {
            _logger.LogInformation("User with email {Email} already exists, skipping seeding", email);
            return;
        }

        try
        {
            // Use the execution strategy for resilient database operations
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1) Create identity user + assign User role
                    var idResult = await _identityService.CreateIdentityUserAsync(email, password, Roles.User);

                    if (idResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create identity user for {Email}: {Error}", email, idResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // 2) Create domain User aggregate (no role assignments)
                    var userResult = User.Create(
                        UserId.Create(idResult.Value),
                        name,
                        email,
                        null,
                        isActive: true);

                    if (userResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create domain user for {Email}: {Error}", email, userResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Clear domain events to avoid producing events during seeding
                    userResult.Value.ClearDomainEvents();

                    // 3) Persist the domain user
                    await _userRepository.AddAsync(userResult.Value);
                    await _context.SaveChangesAsync();

                    // 4) Seed Device and UserDeviceSession
                    var device = await _deviceRepository.GetByDeviceIdAsync(deviceId);
                    if (device == null)
                    {
                        device = await _deviceRepository.AddAsync(
                            deviceId,
                            platform,
                            modelName);
                        await _context.SaveChangesAsync(); // Save device before creating session
                    }

                    await _userDeviceSessionRepository.AddSessionAsync(
                        idResult.Value,
                        device.Id,
                        fcmToken);
                    await _context.SaveChangesAsync(); // Save session

                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully seeded default user {Email} with device {DeviceId} (ID: {UserId})", email, deviceId, idResult.Value);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while seeding default user {Email}", email);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default user {Email}", email);
        }
    }

    private async Task SeedRestaurantDataAsync()
    {
        // Check if restaurants already exist
        if (_context.Restaurants.Any())
        {
            _logger.LogInformation("Restaurants already exist, skipping seeding");
            return;
        }

        await SeedRestaurantWithMenuAsync(
            "YummyZoom Italian",
            "Authentic Italian cuisine with a modern twist",
            "Italian",
            "https://example.com/logo.png");
    }

    private async Task SeedRestaurantWithMenuAsync(string name, string description, string cuisineType, string logoUrl)
    {
        try
        {
            // Use execution strategy for resilient database operations
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Create Restaurant
                    var restaurantResult = Restaurant.Create(
                        name,
                        logoUrl,
                        backgroundImageUrl: null,
                        description,
                        cuisineType,
                        "123 Main St",
                        "Anytown",
                        "CA",
                        "12345",
                        "USA",
                        "+1 (555) 123-4567",
                        "contact@yummyzoom.com",
                        "10:00-22:00");

                    if (restaurantResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create restaurant: {Error}", restaurantResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Verify and accept orders for this restaurant
                    restaurantResult.Value.Verify();
                    restaurantResult.Value.AcceptOrders();

                    // Clear domain events to avoid producing events during seeding
                    restaurantResult.Value.ClearDomainEvents();

                    // Add restaurant to context
                    _context.Restaurants.Add(restaurantResult.Value);

                    await _context.SaveChangesAsync();

                    // 2. Create Menu
                    var menuResult = Menu.Create(
                        restaurantResult.Value.Id,
                        "Main Menu",
                        "Our delicious offerings",
                        isEnabled: true);

                    if (menuResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create menu: {Error}", menuResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Clear domain events to avoid producing events during seeding
                    menuResult.Value.ClearDomainEvents();

                    _context.Menus.Add(menuResult.Value);
                    await _context.SaveChangesAsync();

                    // 3. Create MenuCategory
                    var categoryResult = MenuCategory.Create(
                        menuResult.Value.Id,
                        "Popular Items",
                        1);

                    if (categoryResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create menu category: {Error}", categoryResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Clear domain events to avoid producing events during seeding
                    categoryResult.Value.ClearDomainEvents();

                    _context.MenuCategories.Add(categoryResult.Value);
                    await _context.SaveChangesAsync();

                    // 4. Create MenuItems
                    var item1 = await CreateMenuItem(
                        restaurantResult.Value.Id,
                        categoryResult.Value.Id,
                        "Pizza Margherita",
                        "Classic pizza with tomato, mozzarella, and basil",
                        12.99m,
                        "https://example.com/pizza.jpg");

                    var item2 = await CreateMenuItem(
                        restaurantResult.Value.Id,
                        categoryResult.Value.Id,
                        "Spaghetti Carbonara",
                        "Creamy pasta with pancetta and parmesan",
                        14.99m,
                        "https://example.com/pasta.jpg");

                    var item3 = await CreateMenuItem(
                        restaurantResult.Value.Id,
                        categoryResult.Value.Id,
                        "Tiramisu",
                        "Classic Italian coffee-flavored dessert",
                        7.99m,
                        "https://example.com/dessert.jpg");

                    await transaction.CommitAsync();

                    _logger.LogInformation("Restaurant data seeding completed successfully");
                    _logger.LogInformation("Seeded entities hierarchy:");
                    _logger.LogInformation("- Restaurant: {RestaurantName} (ID: {RestaurantId}, Verified: {IsVerified}, Accepting Orders: {IsAccepting})",
                        restaurantResult.Value.Name, restaurantResult.Value.Id.Value,
                        restaurantResult.Value.IsVerified, restaurantResult.Value.IsAcceptingOrders);
                    _logger.LogInformation("  └─ Menu: {MenuName} (ID: {MenuId})",
                        menuResult.Value.Name, menuResult.Value.Id.Value);
                    _logger.LogInformation("     └─ Category: {CategoryName} (ID: {CategoryId})",
                        categoryResult.Value.Name, categoryResult.Value.Id.Value);

                    if (item1 is not null)
                        _logger.LogInformation("        ├─ MenuItem: {Item1Name} (ID: {Item1Id})",
                            item1.Name, item1.Id.Value);
                    if (item2 is not null)
                        _logger.LogInformation("        ├─ MenuItem: {Item2Name} (ID: {Item2Id})",
                            item2.Name, item2.Id.Value);
                    if (item3 is not null)
                        _logger.LogInformation("        └─ MenuItem: {Item3Name} (ID: {Item3Id})",
                            item3.Name, item3.Id.Value);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while seeding restaurant data: {Message}", ex.Message);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed restaurant data: {Message}", ex.Message);
        }
    }

    private async Task<MenuItem?> CreateMenuItem(
        RestaurantId restaurantId,
        MenuCategoryId categoryId,
        string name,
        string description,
        decimal price,
        string? imageUrl = null)
    {
        var priceObj = new Money(price, "USD");

        var menuItemResult = MenuItem.Create(
            restaurantId,
            categoryId,
            name,
            description,
            priceObj,
            imageUrl);

        if (menuItemResult.IsFailure)
        {
            _logger.LogWarning("Failed to create menu item '{Name}': {Error}", name, menuItemResult.Error);
            return null;
        }

        // Clear domain events to avoid producing events during seeding
        menuItemResult.Value.ClearDomainEvents();

        _context.MenuItems.Add(menuItemResult.Value);
        await _context.SaveChangesAsync();

        return menuItemResult.Value;
    }
}
