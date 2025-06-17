using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IUserAggregateRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default); 
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    // Potentially: Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default);
}
