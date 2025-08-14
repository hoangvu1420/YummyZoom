using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;

namespace YummyZoom.Infrastructure.Data.Inbox;

public sealed class InboxStore : IInboxStore
{
	private readonly ApplicationDbContext _db;

	public InboxStore(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<bool> ExistsAsync(Guid eventId, string handler, CancellationToken ct)
	{
		return await _db.Set<InboxMessage>()
			.AnyAsync(x => x.EventId == eventId && x.Handler == handler, ct);
	}

	public async Task AddAsync(Guid eventId, string handler, string? error, CancellationToken ct)
	{
		await _db.AddAsync(new InboxMessage
		{
			EventId = eventId,
			Handler = handler,
			Error = error
		}, ct);
	}
}
