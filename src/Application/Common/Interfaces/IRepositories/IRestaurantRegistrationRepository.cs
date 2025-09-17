using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.RestaurantRegistrationAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IRestaurantRegistrationRepository
{
    Task<RestaurantRegistration?> GetByIdAsync(RestaurantRegistrationId id, CancellationToken cancellationToken = default);
    Task AddAsync(RestaurantRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateAsync(RestaurantRegistration registration, CancellationToken cancellationToken = default);

    Task<List<RestaurantRegistration>> GetBySubmitterAsync(UserId submitterUserId, CancellationToken cancellationToken = default);
}

