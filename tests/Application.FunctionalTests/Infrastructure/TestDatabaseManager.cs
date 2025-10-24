using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Infrastructure.Persistence.EfCore;

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
    /// Finds an Order entity by its key values, including related entities with split query optimization.
    /// </summary>
    public static async Task<Domain.OrderAggregate.Order?> FindOrderAsync(Domain.OrderAggregate.ValueObjects.OrderId orderId)
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Orders
            .AsSplitQuery()
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    /// <summary>
    /// Finds a TeamCart entity by its key values, including related entities with split query optimization.
    /// </summary>
    public static async Task<Domain.TeamCartAggregate.TeamCart?> FindTeamCartAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId teamCartId)
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.TeamCarts
            .AsSplitQuery()
            .Include(tc => tc.Items)
            .Include(tc => tc.Members)
            .Include(tc => tc.MemberPayments)
            .FirstOrDefaultAsync(tc => tc.Id == teamCartId);
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
    /// Updates an entity in the database.
    /// </summary>
    public static async Task UpdateAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = TestInfrastructure.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Update(entity);
        await context.SaveChangesAsync();
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
