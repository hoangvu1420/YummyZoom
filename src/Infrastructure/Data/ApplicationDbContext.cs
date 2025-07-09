using System.Reflection;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Models;

namespace YummyZoom.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<UserDeviceSession> UserDeviceSessions => Set<UserDeviceSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Apply global query filters for soft delete
        ApplySoftDeleteQueryFilters(builder);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to all entities that implement ISoftDeletableEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            
            if (typeof(ISoftDeletableEntity).IsAssignableFrom(clrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod("Property")?.MakeGenericMethod(typeof(bool));
                var isDeletedProperty = System.Linq.Expressions.Expression.Call(propertyMethodInfo!, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
                var compareExpression = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, isDeletedProperty, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(compareExpression, parameter);
                
                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<Task<Result<T>>> work,
        CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Start EF transaction
            await using var tx = await Database.BeginTransactionAsync(ct);

            // Do the work
            var result = await work();

            if (result.IsFailure)
            {
                await tx.RollbackAsync(ct);
                return Result.Failure<T>(result.Error);
            }

            // Save & commit
            await SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }

    public async Task<Result> ExecuteInTransactionAsync(
        Func<Task<Result>> work,
        CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Start EF transaction
            await using var tx = await Database.BeginTransactionAsync(ct);

            // Do the work
            var result = await work();

            if (result.IsFailure)
            {
                await tx.RollbackAsync(ct);
                return Result.Failure(result.Error);
            }

            // Save & commit
            await SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }
}
