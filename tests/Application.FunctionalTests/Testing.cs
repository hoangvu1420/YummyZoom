using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;

namespace YummyZoom.Application.FunctionalTests;

[SetUpFixture]
public partial class Testing
{
    private static ITestDatabase _database = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private static IServiceScopeFactory _scopeFactory = null!;
    private static Guid? _userId; 

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _database = await TestDatabaseFactory.CreateAsync();

        _factory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString());

        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    // Helper method to unwrap Result<T> to T if needed
    public static async Task<T> SendAndUnwrapAsync<T>(IRequest<Result<T>> request)
    {
        var result = await SendAsync(request);
        return result.ValueOrFail();
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static Guid? GetUserId() 
    {
        return _userId;
    }

    public static void SetUserId(Guid? userId)
    {
        _userId = userId;
        
        // Update the TestUserService with the new user context
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(userId);
    }

    public static async Task RefreshUserClaimsAsync()
    {
        // Refresh claims from the current database state
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        if (_factory?.Services != null)
        {
            await testUserService.RefreshClaimsFromDatabase(_factory.Services);
        }
    }

    public static async Task<Guid> RunAsDefaultUserAsync() 
    {
        return await RunAsUserAsync("test@local", "Testing1234!", Array.Empty<string>());
    }

    public static async Task<Guid> RunAsAdministratorAsync() 
    {
        return await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
    }

    public static async Task<Guid> RunAsUserAsync(string userName, string password, string[] roles) 
    {
        await EnsureRolesExistAsync(roles);
        
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser { UserName = userName, Email = userName };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(Environment.NewLine, result.ToApplicationResult().Errors);
                throw new Exception($"Unable to create {userName}.{Environment.NewLine}{errors}");
            }
        }

        if (roles.Any())
        {
            var userRoles = await userManager.GetRolesAsync(user);
            var missingRoles = roles.Except(userRoles).ToArray();
            if (missingRoles.Any())
            {
                await userManager.AddToRolesAsync(user, missingRoles);
            }
        }

        _userId = user.Id;
        
        // Update the TestUserService with the new user context
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(user.Id);
        
        // Add administrator claims if user has Administrator role
        if (roles.Contains(Roles.Administrator))
        {
            testUserService.AddAdminClaim();
        }
        
        return _userId.Value;
    }

    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();
        }
        catch (Exception) 
        {
        }

        _userId = null;
        
        // Clear the test user service as well
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(null);
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static T GetService<T>()
        where T : notnull
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    public static IServiceScope CreateScope()
    {
        return _scopeFactory.CreateScope();
    }
    
    // Helper to ensure roles exist, can be called by other helpers
    public static async Task EnsureRolesExistAsync(params string[]? roleNames)
    {
        if (roleNames == null || roleNames.Length == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope(); // Essential: new scope for this operation
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName)) continue; // Skip empty role names

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    // Log or handle more gracefully if this is a common test setup issue
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Warning: Failed to create role {roleName} during test setup. Errors: {errors}");
                }
            }
        }
    }
    
    public static async Task SetupForUserRegistrationTestsAsync()
    {
        await EnsureRolesExistAsync(Roles.User, Roles.Administrator, Roles.RestaurantOwner); 
    }

    /// <summary>
    /// Creates a role assignment for a user in a restaurant with the specified role.
    /// Requires an administrator to be logged in.
    /// </summary>
    public static async Task<Guid> CreateRoleAssignmentAsync(Guid userId, Guid restaurantId, RestaurantRole role)
    {
        var command = new CreateRoleAssignmentCommand(userId, restaurantId, role);
        var result = await SendAsync(command);
        
        if (result.IsFailure)
        {
            throw new Exception($"Failed to create role assignment: {result.Error.Description}");
        }
        
        return result.Value.RoleAssignmentId;
    }

    /// <summary>
    /// Sets up a user as a restaurant owner for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    public static async Task<Guid> RunAsRestaurantOwnerAsync(string email, Guid restaurantId)
    {
        // First ensure we have admin access to create role assignments
        await EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await RunAsUserAsync(email, "Password123!", Array.Empty<string>());
        
        // Switch back to admin to create role assignment
        await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
        
        // Create the restaurant owner role assignment
        await CreateRoleAssignmentAsync(userId, restaurantId, RestaurantRole.Owner);
        
        // Switch back to the target user and add the restaurant owner claim
        SetUserId(userId);
        
        // Add the restaurant owner permission claim to the test user service
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.AddPermissionClaim(Roles.RestaurantOwner, restaurantId.ToString());
        
        return userId;
    }

    /// <summary>
    /// Sets up a user as restaurant staff for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    public static async Task<Guid> RunAsRestaurantStaffAsync(string email, Guid restaurantId)
    {
        // First ensure we have admin access to create role assignments
        await EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await RunAsUserAsync(email, "Password123!", Array.Empty<string>());
        
        // Switch back to admin to create role assignment
        await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
        
        // Create the restaurant staff role assignment
        await CreateRoleAssignmentAsync(userId, restaurantId, RestaurantRole.Staff);
        
        // Switch back to the target user and add the restaurant staff claim
        SetUserId(userId);
        
        // Add the restaurant staff permission claim to the test user service
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.AddPermissionClaim(Roles.RestaurantStaff, restaurantId.ToString());
        
        return userId;
    }

    /// <summary>
    /// Sets up a user with multiple restaurant roles for testing complex authorization scenarios.
    /// </summary>
    public static async Task<Guid> RunAsUserWithMultipleRestaurantRolesAsync(string email, (Guid restaurantId, RestaurantRole role)[] roleAssignments)
    {
        // First ensure we have admin access to create role assignments
        await EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await RunAsUserAsync(email, "Password123!", Array.Empty<string>());
        
        // Switch back to admin to create role assignments
        await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
        
        // Create all role assignments
        foreach (var (restaurantId, role) in roleAssignments)
        {
            await CreateRoleAssignmentAsync(userId, restaurantId, role);
        }
        
        // Switch back to the target user and add all permission claims
        SetUserId(userId);
        
        // Add all permission claims to the test user service
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        foreach (var (restaurantId, role) in roleAssignments)
        {
            var roleConstant = role switch
            {
                RestaurantRole.Owner => Roles.RestaurantOwner,
                RestaurantRole.Staff => Roles.RestaurantStaff,
                _ => role.ToString()
            };
            testUserService.AddPermissionClaim(roleConstant, restaurantId.ToString());
        }
        
        return userId;
    }

    /// <summary>
    /// Sets up authorization test environment with required roles.
    /// </summary>
    public static async Task SetupForAuthorizationTestsAsync()
    {
        await EnsureRolesExistAsync(Roles.Administrator, Roles.RestaurantOwner, Roles.RestaurantStaff, Roles.User);
    }

    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await _database.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
