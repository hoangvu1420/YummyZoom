using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure.Database;
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

    /// <summary>
    /// Initializes the test infrastructure before any tests run.
    /// </summary>
    public static async Task RunBeforeAnyTests()
    {
        _database = await TestDatabaseFactory.CreateAsync();
        _factory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString());
        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests complete.
    /// </summary>
    public static async Task RunAfterAnyTests()
    {
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
    /// Resets the test state by resetting the database.
    /// </summary>
    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();
        }
        catch (Exception)
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
    internal static IServiceScopeFactory GetScopeFactory() => _scopeFactory;
}