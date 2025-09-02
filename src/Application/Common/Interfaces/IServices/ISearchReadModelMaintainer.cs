namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface ISearchReadModelMaintainer
{
    Task UpsertRestaurantByIdAsync(Guid restaurantId, long sourceVersion, CancellationToken ct = default);
    Task UpsertMenuItemByIdAsync(Guid menuItemId, long sourceVersion, CancellationToken ct = default);
    Task SoftDeleteByIdAsync(Guid id, long sourceVersion, CancellationToken ct = default);
}

