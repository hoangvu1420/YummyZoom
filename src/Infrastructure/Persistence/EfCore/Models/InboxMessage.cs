namespace YummyZoom.Infrastructure.Persistence.EfCore.Models;

public sealed class InboxMessage
{
	public Guid EventId { get; init; }
	public string Handler { get; init; } = default!;
	public DateTime ProcessedOnUtc { get; init; } = DateTime.UtcNow;
	public string? Error { get; init; }
}
