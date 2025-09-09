using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public sealed class InboxStore : IInboxStore
{
	private readonly ApplicationDbContext _dbContext;

	public InboxStore(ApplicationDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<bool> ExistsAsync(Guid eventId, string handler, CancellationToken ct)
	{
		return await _dbContext.Set<InboxMessage>()
			.AnyAsync(x => x.EventId == eventId && x.Handler == handler, ct);
	}

	public async Task AddAsync(Guid eventId, string handler, string? error, CancellationToken ct)
	{
		await _dbContext.AddAsync(new InboxMessage
		{
			EventId = eventId,
			Handler = handler,
			Error = error
		}, ct);
	}
}
