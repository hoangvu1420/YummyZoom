using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure.Database;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests.Infrastructure;

/// <summary>
/// Core test infrastructure responsible for setup, teardown, and service provider management.
/// </summary>
public static class TestInfrastructure
{
    private static ITestDatabase _database = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private static IServiceScopeFactory _scopeFactory = null!;
    
    // Service replacement tracking
    private static readonly Dictionary<Type, object> _serviceReplacements = new();
    private static CustomWebApplicationFactory? _customFactory;

    /// <summary>
    /// Initializes the test infrastructure before any tests run.
    /// </summary>
    public static async Task RunBeforeAnyTests()
    {
        _database = await TestDatabaseFactory.CreateAsync();
        _factory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString());
        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        
        // Initialize the test data factory with default entities
        await TestDataFactory.InitializeAsync();
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests complete.
    /// </summary>
    public static async Task RunAfterAnyTests()
    {
        // Reset the test data factory state
        TestDataFactory.Reset();
        
        await _database.DisposeAsync();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Sends a request through MediatR and returns the response.
    /// </summary>
    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        return await mediator.Send(request);
    }

    /// <summary>
    /// Sends a request through MediatR and unwraps the Result<T> to T.
    /// </summary>
    public static async Task<T> SendAndUnwrapAsync<T>(IRequest<Result<T>> request)
    {
        var result = await SendAsync(request);
        return result.ValueOrFail();
    }

    /// <summary>
    /// Sends a request through MediatR without expecting a response.
    /// </summary>
    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        await mediator.Send(request);
    }

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a new service scope for dependency injection.
    /// </summary>
    public static IServiceScope CreateScope()
    {
        return _scopeFactory.CreateScope();
    }

    /// <summary>
    /// Resets the test state by resetting the database and clearing service replacements.
    /// </summary>
    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();

            // Ensure test data is restored after database reset
            await TestDataFactory.EnsureTestDataAsync();
            
            // Clear service replacements to prevent leakage between test classes
            await ResetServiceReplacements();
        }
        catch 
        {
            // Silently handle database reset failures
        }
    }

    /// <summary>
    /// Gets the current test database instance.
    /// </summary>
    internal static ITestDatabase GetDatabase() => _database;

    /// <summary>
    /// Gets the current web application factory instance.
    /// </summary>
    internal static CustomWebApplicationFactory GetFactory() => _factory;

    /// <summary>
    /// Gets the current service scope factory instance.
    /// </summary>
    internal static IServiceScopeFactory GetScopeFactory() => _customFactory?.Services.GetRequiredService<IServiceScopeFactory>() ?? _scopeFactory;

    #region Service Replacement

    /// <summary>
    /// Replaces a service with a specific implementation instance.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <param name="implementation">The implementation instance</param>
    public static void ReplaceService<TInterface>(TInterface implementation)
        where TInterface : class
    {
        _serviceReplacements[typeof(TInterface)] = implementation;
        RebuildContainerWithReplacements();
    }

    /// <summary>
    /// Replaces a service with a specific implementation type.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <typeparam name="TImplementation">The implementation type</typeparam>
    public static void ReplaceService<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _serviceReplacements[typeof(TInterface)] = typeof(TImplementation);
        RebuildContainerWithReplacements();
    }

    /// <summary>
    /// Clears all service replacements and resets to original factory.
    /// </summary>
    public static async Task ResetServiceReplacements()
    {
        _serviceReplacements.Clear();
        
        if (_customFactory != null)
        {
            await _customFactory.DisposeAsync();
            _customFactory = null;
        }
        
        // Reset scope factory to use original factory
        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Rebuilds the DI container with current service replacements.
    /// </summary>
    private static void RebuildContainerWithReplacements()
    {
        if (_serviceReplacements.Count == 0)
        {
            // No replacements needed, use original factory
            _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
            return;
        }

        // Dispose existing custom factory
        _customFactory?.Dispose();

        // Create new factory with replacements
        _customFactory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString(), _serviceReplacements);
        
        // Update the scope factory to use the custom factory with service replacements
        _scopeFactory = _customFactory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    #endregion
}
