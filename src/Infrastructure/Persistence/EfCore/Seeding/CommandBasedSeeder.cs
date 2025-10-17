using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using System.Security.Claims;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

/// <summary>
/// Base class for seeders that use Application layer commands via MediatR.
/// Provides helper methods for command execution with proper authentication context.
/// </summary>
public abstract class CommandBasedSeeder : ISeeder
{
    protected readonly ISender Sender;
    protected readonly ILogger Logger;
    protected readonly ApplicationDbContext DbContext;

    protected CommandBasedSeeder(ISender sender, ILogger logger, ApplicationDbContext dbContext)
    {
        Sender = sender;
        Logger = logger;
        DbContext = dbContext;
    }

    public abstract string Name { get; }
    public abstract int Order { get; }

    public abstract Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default);
    public abstract Task<SharedKernel.Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with a temporary user context for seeding purposes.
    /// </summary>
    protected async Task<TResponse> ExecuteCommandAsUserAsync<TResponse>(
        IRequest<TResponse> command,
        UserId userId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Get the IUser service from the service provider
        var userService = serviceProvider.GetRequiredService<IUser>();
        
        // Store original user context
        var originalUserId = userService.DomainUserId;
        var originalId = userService.Id;
        
        try
        {
            // Temporarily set the seeding user context
            // Note: This approach requires IUser to be implemented in a way that allows mutation
            // or we need a custom IUser implementation for seeding
            
            // For now, we'll use a scoped service approach
            // The actual implementation will depend on how IUser is registered
            
            return await Sender.Send(command, cancellationToken);
        }
        finally
        {
            // Restore original context if needed
            // This part may need adjustment based on actual IUser implementation
        }
    }

    /// <summary>
    /// Logs seeding progress with consistent formatting.
    /// </summary>
    protected void LogProgress(string message, params object[] args)
    {
        Logger.LogInformation($"[{Name}] {message}", args);
    }

    /// <summary>
    /// Logs seeding warnings with consistent formatting.
    /// </summary>
    protected void LogWarning(string message, params object[] args)
    {
        Logger.LogWarning($"[{Name}] {message}", args);
    }

    /// <summary>
    /// Logs seeding errors with consistent formatting.
    /// </summary>
    protected void LogError(Exception? exception, string message, params object[] args)
    {
        if (exception != null)
            Logger.LogError(exception, $"[{Name}] {message}", args);
        else
            Logger.LogError($"[{Name}] {message}", args);
    }
}

/// <summary>
/// Implementation of IUser for seeding context.
/// This allows commands to be executed with a specific user context during seeding.
/// </summary>
public class SeedingUserContext : IUser
{
    private UserId? _userId;
    private string? _id;
    private ClaimsPrincipal? _principal;

    public string? Id => _id;
    public UserId? DomainUserId => _userId;
    public ClaimsPrincipal? Principal => _principal;
    public string? Email { get; private set; }
    public bool IsAuthenticated => _userId != null;

    public void SetUser(UserId userId, string email = "seeding@localhost")
    {
        _userId = userId;
        _id = userId.Value.ToString();
        Email = email;
        
        // Create a basic ClaimsPrincipal for the seeding user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "SeedingAuthentication");
        _principal = new ClaimsPrincipal(identity);
    }

    public void Clear()
    {
        _userId = null;
        _id = null;
        Email = null;
        _principal = null;
    }
}
