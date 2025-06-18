using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel.Models;

namespace YummyZoom.Infrastructure.Data;

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
            "cQ9KY8l7TxSVFN0p-Z36rx:APA91bESwLQivfzOkzxnOviBsDNIl4wuDcBRI8j39x5zyi87R3fVS7g4lkLhdU6iXSVg6vQSk_ShL_E5f84mf4oSZaD92AAoEcim0VFYsTUoebhCF82P4Xk",
            "Android", "seed-device-2", "Seed Device 2");

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            var todoList = TodoList.Create("Todo List", Color.White);
            todoList.AddItem(TodoItem.Create("Make a todo list 📃", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Check off the first item ✅", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Realise you've already done two things on the list! 🤯", null, PriorityLevel.None, null));
            todoList.AddItem(TodoItem.Create("Reward yourself with a nice, long nap 🏆", null, PriorityLevel.None, null));

            _context.TodoLists.Add(todoList);
            await _context.SaveChangesAsync();
        }

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

                    // 3) Persist the domain user
                    await _userRepository.AddAsync(userResult.Value);
                    await _context.SaveChangesAsync();

                    // 4) Seed Device and UserDeviceSession
                    var device = await _deviceRepository.GetByDeviceIdAsync(deviceId);
                    if (device == null)
                    {
                        device = new Device
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceId,
                            Platform = platform,
                            ModelName = modelName,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _deviceRepository.AddAsync(device);
                        await _context.SaveChangesAsync(); // Save device before creating session
                    }

                    var newSession = new UserDeviceSession
                    {
                        Id = Guid.NewGuid(),
                        UserId = idResult.Value,
                        DeviceId = device.Id,
                        FcmToken = fcmToken,
                        IsActive = true,
                        LastLoginAt = DateTime.UtcNow,
                        LoggedOutAt = null
                    };
                    await _userDeviceSessionRepository.AddSessionAsync(newSession);
                    await _context.SaveChangesAsync(); // Save session

                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully seeded default user {Email} with device {DeviceId}", email, deviceId);
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
}
