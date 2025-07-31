using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Infrastructure.Data;

namespace YummyZoom.Application.FunctionalTests.Infrastructure;

/// <summary>
/// Database-specific operations and entity management for tests.
/// </summary>
public static class TestDatabaseManager
{
    /// <summary>
    /// Finds an entity by its key values.
    /// </summary>
    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.FindAsync<TEntity>(keyValues);
    }

    /// <summary>
    /// Adds an entity to the database.
    /// </summary>
    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Add(entity);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Counts the number of entities of the specified type.
    /// </summary>
    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<TEntity>().CountAsync();
    }

    /// <summary>
    /// Gets the database context for advanced operations.
    /// </summary>
    public static ApplicationDbContext GetContext(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Executes a database operation within a scope.
    /// </summary>
    public static async Task<T> ExecuteInScopeAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await operation(context);
    }

    /// <summary>
    /// Executes a database operation within a scope without return value.
    /// </summary>
    public static async Task ExecuteInScopeAsync(Func<ApplicationDbContext, Task> operation)
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await operation(context);
    }
}