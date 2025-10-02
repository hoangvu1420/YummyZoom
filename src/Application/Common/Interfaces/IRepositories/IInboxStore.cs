namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid eventId, string handler, CancellationToken ct);
    Task AddAsync(Guid eventId, string handler, string? error, CancellationToken ct);
}
