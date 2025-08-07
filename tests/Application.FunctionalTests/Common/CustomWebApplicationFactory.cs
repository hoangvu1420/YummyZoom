using System.Data.Common;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private readonly string _connectionString;
    private readonly Dictionary<Type, object>? _serviceReplacements;
    
    // Static singleton instance to ensure consistency across all scopes
    private static readonly TestUserService _testUserService = new();

    public CustomWebApplicationFactory(DbConnection connection, string connectionString)
    {
        _connection = connection;
        _connectionString = connectionString;
        _serviceReplacements = null;
    }

    public CustomWebApplicationFactory(DbConnection connection, string connectionString, Dictionary<Type, object> serviceReplacements)
    {
        _connection = connection;
        _connectionString = connectionString;
        _serviceReplacements = serviceReplacements;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:YummyZoomDb", _connectionString);
        builder.ConfigureTestServices(services =>
        {
            ConfigureTestUserService(services);
            ConfigureMockServices(services);
            ConfigureTestCommandHandlers(services);
            services.ApplyServiceReplacements(_serviceReplacements);
        });
    }

    /// <summary>
    /// Configures the test user service for authentication testing.
    /// </summary>
    private void ConfigureTestUserService(IServiceCollection services)
    {
        services
            .RemoveAll<IUser>()
            .AddSingleton<IUser>(_testUserService);
    }

    /// <summary>
    /// Configures mock services for testing.
    /// </summary>
    private static void ConfigureMockServices(IServiceCollection services)
    {
        ConfigureFcmServiceMock(services);
    }

    /// <summary>
    /// Configures FCM service mock for notification testing.
    /// </summary>
    private static void ConfigureFcmServiceMock(IServiceCollection services)
    {
        services
            .RemoveAll<IFcmService>()
            .AddTransient(provider =>
            {
                var mock = new Mock<IFcmService>();
                
                // Mock successful multicast notification sending
                mock.Setup(s => s.SendMulticastNotificationAsync(
                        It.IsAny<IEnumerable<string>>(), 
                        It.IsAny<string>(), 
                        It.IsAny<string>(), 
                        It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(Result.Success<List<string>>(new List<string>()));
                
                // Mock successful single notification sending
                mock.Setup(s => s.SendNotificationAsync(
                        It.IsAny<string>(), 
                        It.IsAny<string>(), 
                        It.IsAny<string>(), 
                        It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(Result.Success());
                    
                // Mock successful data message sending
                mock.Setup(s => s.SendDataMessageAsync(
                        It.IsAny<string>(), 
                        It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(Result.Success());
                        
                return mock.Object;
            });
    }

    /// <summary>
    /// Configures test command handlers for authorization testing.
    /// </summary>
    private static void ConfigureTestCommandHandlers(IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<TestRestaurantOwnerCommand, Result<Unit>>, TestRestaurantOwnerCommandHandler>();
        services.AddTransient<IRequestHandler<TestRestaurantStaffCommand, Result<Unit>>, TestRestaurantStaffCommandHandler>();
        services.AddTransient<IRequestHandler<TestUnprotectedCommand, Result<Unit>>, TestUnprotectedCommandHandler>();
        services.AddTransient<IRequestHandler<TestUserOwnerCommand, Result<Unit>>, TestUserOwnerCommandHandler>();
        services.AddTransient<IRequestHandler<TestUnprotectedUserCommand, Result<Unit>>, TestUnprotectedUserCommandHandler>();
    }
    
    // Static method to access the test user service from test methods
    public static TestUserService GetTestUserService() => _testUserService;
}

/// <summary>
/// Test-specific implementation of IUser that can be updated dynamically during tests
/// </summary>
public class TestUserService : IUser
{
    private Guid? _userId;
    
    private readonly List<Claim> _additionalClaims = new();

    public string? Id => _userId?.ToString();

    public UserId? DomainUserId => _userId.HasValue ? UserId.Create(_userId.Value) : null;

    public ClaimsPrincipal? Principal
    {
        get
        {
            if (!_userId.HasValue)
                return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.Value.ToString()),
                new Claim(ClaimTypes.Name, "test@example.com")
                // Note: UserOwner claims are added automatically by the real system during login
                // For tests, we only add explicit claims that are set up during test scenarios
            };

            // Add any additional claims that were set during test setup
            claims.AddRange(_additionalClaims);

            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }
    }

    public void SetUserId(Guid? userId)
    {
        _userId = userId;
        _additionalClaims.Clear(); // Clear previous claims when switching users
        
        // Automatically add UserOwner claim for the user (this simulates what happens in real authentication)
        if (userId.HasValue)
        {
            _additionalClaims.Add(new Claim("permission", $"{Roles.UserOwner}:{userId.Value}"));
        }
    }

    public void AddPermissionClaim(string role, string resourceId)
    {
        var claimValue = $"{role}:{resourceId}";
        _additionalClaims.Add(new Claim("permission", claimValue));
    }

    public void AddAdminClaim()
    {
        // Add both permission claim for policy-based authorization
        _additionalClaims.Add(new Claim("permission", $"{Roles.UserAdmin}:*"));
        
        // Add role claim for role-based authorization
        _additionalClaims.Add(new Claim(ClaimTypes.Role, Roles.Administrator));
    }

    public void AddRoleClaim(string role)
    {
        _additionalClaims.Add(new Claim(ClaimTypes.Role, role));
    }

    public void RemovePermissionClaim(string role, string resourceId)
    {
        var claimValue = $"{role}:{resourceId}";
        _additionalClaims.RemoveAll(c => c.Type == "permission" && c.Value == claimValue);
    }

    public async Task RefreshClaimsFromDatabase(IServiceProvider serviceProvider)
    {
        if (!_userId.HasValue) return;

        // Clear existing permission claims (but keep role claims)
        _additionalClaims.RemoveAll(c => c.Type == "permission");

        // Re-add UserOwner claim
        _additionalClaims.Add(new Claim("permission", $"{Roles.UserOwner}:{_userId.Value}"));

        // Fetch current role assignments from database and add permission claims
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetService<IRoleAssignmentRepository>();
        if (repository != null)
        {
            try
            {
                var domainUserId = UserId.Create(_userId.Value);
                var roleAssignments = await repository.GetByUserIdAsync(domainUserId);
                
                foreach (var assignment in roleAssignments)
                {
                    var roleConstant = assignment.Role switch
                    {
                        RestaurantRole.Owner => Roles.RestaurantOwner,
                        RestaurantRole.Staff => Roles.RestaurantStaff,
                        _ => assignment.Role.ToString()
                    };
                    
                    var claimValue = $"{roleConstant}:{assignment.RestaurantId.Value}";
                    _additionalClaims.Add(new Claim("permission", claimValue));
                }
            }
            catch
            {
                // If we can't refresh from database, continue with existing claims
            }
        }
    }
}

/// <summary>
/// Extensions for the CustomWebApplicationFactory to handle service replacements.
/// </summary>
public static class CustomWebApplicationFactoryExtensions
{
    /// <summary>
    /// Applies service replacements if any are configured.
    /// </summary>
    public static void ApplyServiceReplacements(this IServiceCollection services, Dictionary<Type, object>? serviceReplacements)
    {
        if (serviceReplacements == null || serviceReplacements.Count == 0)
            return;

        foreach (var replacement in serviceReplacements)
        {
            var serviceType = replacement.Key;
            var implementation = replacement.Value;

            // Remove existing registrations
            services.RemoveAll(serviceType);

            // Add replacement based on type
            if (implementation is Type implementationType)
            {
                // Type-based replacement
                services.AddTransient(serviceType, implementationType);
            }
            else
            {
                // Instance-based replacement
                services.AddSingleton(serviceType, implementation);
            }
        }
    }
}
