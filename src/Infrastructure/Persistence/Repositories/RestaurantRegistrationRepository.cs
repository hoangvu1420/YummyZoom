using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class RestaurantRegistrationRepository : IRestaurantRegistrationRepository
{
    private readonly ApplicationDbContext _db;

    public RestaurantRegistrationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RestaurantRegistration?> GetByIdAsync(RestaurantRegistrationId id, CancellationToken cancellationToken = default)
    {
        return await _db.Set<RestaurantRegistration>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(RestaurantRegistration registration, CancellationToken cancellationToken = default)
    {
        await _db.Set<RestaurantRegistration>().AddAsync(registration, cancellationToken);
    }

    public Task UpdateAsync(RestaurantRegistration registration, CancellationToken cancellationToken = default)
    {
        _db.Set<RestaurantRegistration>().Update(registration);
        return Task.CompletedTask;
    }

    public async Task<List<RestaurantRegistration>> GetBySubmitterAsync(UserId submitterUserId, CancellationToken cancellationToken = default)
    {
        return await _db.Set<RestaurantRegistration>()
            .Where(r => r.SubmitterUserId == submitterUserId)
            .OrderByDescending(r => r.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

