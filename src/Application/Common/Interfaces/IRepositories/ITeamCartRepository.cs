using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface ITeamCartRepository
{
    Task<TeamCart?> GetByIdAsync(TeamCartId id, CancellationToken cancellationToken = default);

    Task AddAsync(TeamCart cart, CancellationToken cancellationToken = default);

    Task UpdateAsync(TeamCart cart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns TeamCarts that are candidates for expiration: status Open or Locked and ExpiresAt <= cutoff.
    /// Intended for background expiration processing.
    /// </summary>
    Task<IReadOnlyList<TeamCart>> GetExpiringCartsAsync(DateTime cutoffUtc, int take, CancellationToken cancellationToken = default);
}
