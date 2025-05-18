using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore; // Required for EF Core async methods

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class UserAggregateRepository : IUserAggregateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserAggregateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.DomainUsers.AddAsync(user, cancellationToken);
        // Note: SaveChangesAsync is typically called by a Unit of Work pattern
        // or explicitly after the command handler completes its operations.
        // Adding it here would make the repository less flexible if multiple
        // operations need to be part of the same transaction.
        // For now, we assume SaveChangesAsync is handled elsewhere (e.g., by IdentityService's transaction).
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Ensure case-insensitive comparison if needed by the database collation or requirements
        return await _dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
    }

    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // FindAsync is suitable for finding by primary key
        return await _dbContext.DomainUsers.FindAsync([userId], cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // EF Core tracks changes, so just marking the entity as Modified
        // or relying on change tracking is usually sufficient.
        // The actual update SQL is generated during SaveChangesAsync.
        _dbContext.DomainUsers.Update(user); 
        // Again, SaveChangesAsync is assumed to be handled elsewhere.
        return Task.CompletedTask; // Update itself is synchronous in terms of EF tracking
    }
}
