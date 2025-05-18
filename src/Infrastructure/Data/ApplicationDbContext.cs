using System.Reflection;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<User> DomainUsers => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
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
