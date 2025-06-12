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
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

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
    private readonly IUserDeviceRepository _userDeviceRepository;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger, 
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole<Guid>> roleManager,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IUserDeviceRepository userDeviceRepository)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _identityService = identityService;
        _userRepository = userRepository;
        _userDeviceRepository = userDeviceRepository;
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
        string[] roles = [Roles.Administrator, Roles.Customer, Roles.RestaurantOwner];

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

        // Customer users - using improved approach following RegisterUserCommandHandler pattern
        await SeedCustomerUserAsync("User 1", "hoangnguyenvu1420@gmail.com", "123456",
            "cgFolBPZTJKuo3zcffqMoz:APA91bGbETXlfLmbXOVTkI8H4NLO7c_FiWjH7mAUzS881EQhIoF0EAE-1wp8FI0aT2a-jY89ji4nvfD3kcWvQiJ1IwUDPxAc_y0NWK4Q0TxEqzWQsUeRNc8",
            "Android", null);

        await SeedCustomerUserAsync("User 2", "hoangnguyenvu1220@gmail.com", "123456",
            "cWJlZpHzTmOXbfn2yQrt3a:APA91bF5cWnD1sOgRszQvK2tt422ApMGrbCoZv1y7krz7VisPFuR4Uaym-w7eX5uk_3TyWcEE61apPECErlwnNhKHpuGlmL5Wwu0NrCRt1LNxefcZxVz7zc",
            "Android", null);

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

    private async Task SeedCustomerUserAsync(string name, string email, string password, string fcmToken, string platform, string? deviceId)
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
                    // 1) Create identity user + assign Customer role
                    var idResult = await _identityService.CreateIdentityUserAsync(email, password, Roles.Customer);
                    
                    if (idResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create identity user for {Email}: {Error}", email, idResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // 2) Build domain User aggregate
                    var roleResult = RoleAssignment.Create(Roles.Customer);
                    if (roleResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create role assignment for {Email}: {Error}", email, roleResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    var userResult = User.Create(
                        UserId.Create(idResult.Value),
                        name,
                        email,
                        null,
                        new List<RoleAssignment> { roleResult.Value });
                        
                    if (userResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to create domain user for {Email}: {Error}", email, userResult.Error);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // 3) Persist the domain user
                    await _userRepository.AddAsync(userResult.Value);
                    await _context.SaveChangesAsync();

                    // 4) Register device for the user
                    await _userDeviceRepository.AddOrUpdateAsync(
                        UserId.Create(idResult.Value),
                        fcmToken.Trim(),
                        platform.Trim(),
                        deviceId?.Trim());
                    
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Successfully seeded customer user {Email} with device", email);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while seeding customer user {Email}", email);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed customer user {Email}", email);
        }
    }
}
