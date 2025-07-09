using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Extensions;

// Required for EF Core async methods

namespace YummyZoom.Infrastructure.Data.Repositories;

public class UserAggregateRepository : IUserAggregateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserAggregateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.DomainUsers.AddAsync(user, cancellationToken);
    }

    /// <summary>
    /// Gets a user by email address. Excludes soft-deleted users by default.
    /// </summary>
    /// <param name="email">The email address to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found and not soft-deleted</returns>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Ensure case-insensitive comparison if needed by the database collation or requirements
        // Global query filter automatically excludes soft-deleted users
        return await _dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
    }

    /// <summary>
    /// Gets a user by ID. Excludes soft-deleted users by default.
    /// </summary>
    /// <param name="userId">The user ID to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found and not soft-deleted</returns>
    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // FindAsync is suitable for finding by primary key
        // Global query filter automatically excludes soft-deleted users
        return await _dbContext.DomainUsers.FindAsync([userId], cancellationToken);
    }

    /// <summary>
    /// Updates an existing user in the repository.
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.DomainUsers.Update(user); 
        return Task.CompletedTask; 
    }

    /// <summary>
    /// Soft deletes a user by setting IsDeleted = true and tracking deletion metadata.
    /// The user will be excluded from normal queries due to global query filters.
    /// </summary>
    /// <param name="user">The user to soft delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task SoftDeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        user.MarkAsDeleted(DateTimeOffset.UtcNow);
        _dbContext.DomainUsers.Update(user);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Restores a soft-deleted user by setting IsDeleted = false.
    /// </summary>
    /// <param name="user">The user to restore</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task RestoreAsync(User user, CancellationToken cancellationToken = default)
    {
        user.Restore();
        _dbContext.DomainUsers.Update(user);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a user by ID including soft-deleted users.
    /// Use this when you need to access deleted users for admin purposes.
    /// </summary>
    /// <param name="userId">The user ID to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, including soft-deleted users</returns>
    public async Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DomainUsers
            .IncludeSoftDeleted()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Gets a user by email including soft-deleted users.
    /// Use this when you need to access deleted users for admin purposes.
    /// </summary>
    /// <param name="email">The email to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, including soft-deleted users</returns>
    public async Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DomainUsers
            .IncludeSoftDeleted()
            .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
    }

    /// <summary>
    /// Performs a hard delete of the user entity from the database.
    /// This permanently removes the user and should only be used for administrative cleanup.
    /// Warning: This action cannot be undone.
    /// </summary>
    /// <param name="user">The user to permanently delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.DomainUsers.Remove(user);
        return Task.CompletedTask;
    }
}
