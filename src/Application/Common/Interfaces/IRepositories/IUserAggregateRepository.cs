using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IUserAggregateRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a user by setting IsDeleted to true and recording deletion metadata.
    /// The user will be excluded from normal queries due to global query filters.
    /// </summary>
    Task SoftDeleteAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted user by setting IsDeleted to false and clearing deletion metadata.
    /// </summary>
    Task RestoreAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID including soft-deleted entities. Use this when you need to access deleted users.
    /// </summary>
    Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email including soft-deleted entities. Use this when you need to access deleted users.
    /// </summary>
    Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a user from the database. This is irreversible and should only be used for compliance purposes.
    /// </summary>
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);

    // Potentially: Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default);
}
