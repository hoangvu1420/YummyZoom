using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

// Required for EF Core async methods

namespace YummyZoom.Infrastructure.Data.Repositories;

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
        _dbContext.DomainUsers.Update(user); 
        return Task.CompletedTask; 
    }
}
