namespace YummyZoom.Application.Restaurants.Queries.Common;

public interface IMenuReadModelRebuilder
{
    Task<(string menuJson, DateTimeOffset lastRebuiltAt)> RebuildAsync(Guid restaurantId, CancellationToken ct = default);
    Task UpsertAsync(Guid restaurantId, string menuJson, DateTimeOffset lastRebuiltAt, CancellationToken ct = default);
    Task DeleteAsync(Guid restaurantId, CancellationToken ct = default);
}
