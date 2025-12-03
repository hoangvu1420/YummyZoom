using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Security;

public interface IDevImpersonationService
{
    Task RunAsRestaurantStaffAsync(Guid restaurantId, IServiceScopeFactory scopeFactory, Func<IServiceProvider, Task> action, CancellationToken ct = default);
    Task RunAsUserAsync(Guid userId, IServiceScopeFactory scopeFactory, Func<IServiceProvider, Task> action, IEnumerable<string>? permissionClaims = null, CancellationToken ct = default);
}

/// <summary>
/// Dev/Test only helper to impersonate a restaurant staff principal for the duration of an action.
/// It assigns a synthetic HttpContext with the required permission claim so Application handlers authorize.
/// </summary>
public sealed class DevImpersonationService : IDevImpersonationService
{
    public async Task RunAsRestaurantStaffAsync(Guid restaurantId, IServiceScopeFactory scopeFactory, Func<IServiceProvider, Task> action, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var httpAccessor = services.GetRequiredService<IHttpContextAccessor>();

        var identity = new ClaimsIdentity("DevImpersonation");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, "dev-simulated-staff@yummyzoom.test"));
        // Permission claim format consumed in query handlers / behaviors
        identity.AddClaim(new Claim("permission", $"{Roles.RestaurantStaff}:{restaurantId}"));

        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        // Assign synthetic HttpContext for this scope
        var original = httpAccessor.HttpContext;
        try
        {
            httpAccessor.HttpContext = httpContext;
            await action(services);
        }
        finally
        {
            httpAccessor.HttpContext = original;
        }
    }

    public async Task RunAsUserAsync(Guid userId, IServiceScopeFactory scopeFactory, Func<IServiceProvider, Task> action, IEnumerable<string>? permissionClaims = null, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var httpAccessor = services.GetRequiredService<IHttpContextAccessor>();

        var identity = new ClaimsIdentity("DevImpersonation");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, $"dev-simulated-user-{userId}@yummyzoom.test"));
        
        // Add permission claims if provided
        if (permissionClaims != null)
        {
            foreach (var permission in permissionClaims)
            {
                identity.AddClaim(new Claim("permission", permission));
            }
        }

        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        // Assign synthetic HttpContext for this scope
        var original = httpAccessor.HttpContext;
        try
        {
            httpAccessor.HttpContext = httpContext;
            await action(services);
        }
        finally
        {
            httpAccessor.HttpContext = original;
        }
    }
}

