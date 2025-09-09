using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class RestaurantAccountRepository : IRestaurantAccountRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RestaurantAccountRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RestaurantAccount?> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        // One account per restaurant enforced via unique index.
        return await _dbContext.RestaurantAccounts
            .FirstOrDefaultAsync(a => a.RestaurantId == restaurantId, cancellationToken);
    }

    public async Task<RestaurantAccount> GetOrCreateAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        // Use PostgreSQL's UPSERT to handle race conditions atomically
        var sql = """
            INSERT INTO "RestaurantAccounts" ("Id", "RestaurantId", "CurrentBalance_Amount", 
            "CurrentBalance_Currency", "Created")
            VALUES ({0}, {1}, 0.00, 'USD', {2})
            ON CONFLICT ("RestaurantId") DO NOTHING;
            """;

        var newId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        
        await _dbContext.Database.ExecuteSqlRawAsync(sql, [newId, restaurantId.Value, createdAt], cancellationToken);
        
        // Return the account (whether we just created it or it already existed)
        return await GetByRestaurantIdAsync(restaurantId, cancellationToken) 
            ?? throw new InvalidOperationException($"Failed to get or create RestaurantAccount for {restaurantId}");
    }

    public async Task AddAsync(RestaurantAccount account, CancellationToken cancellationToken = default)
    {
        await _dbContext.RestaurantAccounts.AddAsync(account, cancellationToken);
    }

    public Task UpdateAsync(RestaurantAccount account, CancellationToken cancellationToken = default)
    {
        _dbContext.RestaurantAccounts.Update(account);
        return Task.CompletedTask;
    }
}
